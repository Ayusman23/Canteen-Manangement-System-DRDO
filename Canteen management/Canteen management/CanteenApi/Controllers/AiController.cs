using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Canteen.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IApplicationDbContext context,
        ILogger<AiController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    [HttpPost("mess-assistant")]
    public async Task<IActionResult> MessAssistant([FromBody] AiMessAssistantRequest request)
    {
        var apiKey = _configuration["GEMINI_API_KEY"];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Fetch current active menu for context
        var schedules = await _context.DailyMenuSchedules
            .Include(s => s.MenuItem)
            .Where(s => s.Date == today && s.IsActive)
            .ToListAsync();

        var menuSummary = schedules.Count > 0
            ? string.Join(", ", schedules.Select(s => $"{s.MealType}: {s.MenuItem?.Name} (₹{s.Price})"))
            : "Standard Nutritious Defense Mess Meal (Rice, Dal, Seasonal Vegetable, Roti, Salad)";

        var userQuestion = string.IsNullOrWhiteSpace(request.Prompt)
            ? "What should I eat today for high energy during lab research?"
            : request.Prompt;

        var systemPrompt = $@"
You are the DRDO Canteen Nutrition & Logistics AI Advisor for defence scientists, engineers, and armed forces personnel.
Today is {today:dddd, MMMM dd, yyyy}.
Today's Canteen Mess Menu:
{menuSummary}

Personnel Query: ""{userQuestion}""
Personnel Dietary Goal: ""{request.DietaryGoal ?? "Balanced Energy"}""

Provide a concise, professional, military-enclave friendly nutrition recommendation in 2 to 3 punchy paragraphs:
1. Recommended meal combo from today's menu with estimated caloric/macronutrient value.
2. Health benefits tailored to defence laboratory work / mental stamina.
3. Quick hydration & dining advice.
";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var geminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = systemPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 600
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(geminiEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var resJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resJson);
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var text = candidates[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        return Ok(new
                        {
                            Success = true,
                            Response = text,
                            TodayMenu = menuSummary,
                            Provider = "Google Gemini 1.5 Flash"
                        });
                    }
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini API call returned {Status}: {Error}. Falling back to deterministic advisor.", response.StatusCode, errorDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reach Gemini API. Falling back to local advisory.");
            }
        }

        // Rule-based fallback response
        var fallbackResponse = $@"
Based on today's mess menu ({menuSummary}), the Chief Dietary Officer recommends opting for the Normal Thali paired with fresh salad.
- **Macronutrients**: ~650 kcal, 22g protein, complex carbohydrates for sustained research focus without post-lunch lethargy.
- **Defence Enclave Tip**: Ensure pickup before 13:30 hrs to enjoy freshly steamed courses. Remember to stay hydrated during laser & avionics bench experiments.";

        return Ok(new
        {
            Success = true,
            Response = fallbackResponse,
            TodayMenu = menuSummary,
            Provider = "DRDO Nutritional Knowledge Base (Local)"
        });
    }
}

public class AiMessAssistantRequest
{
    public string? Prompt { get; set; }
    public string? DietaryGoal { get; set; }
}
