using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CanteenApi.Services;

public interface IPaymentService
{
    Task<RazorpayOrderResult> CreateOrderAsync(decimal amountInInr, string receipt, string currency = "INR");
    bool VerifySignature(string orderId, string paymentId, string signature);
}

public record RazorpayOrderResult(
    string OrderId,
    decimal Amount,
    string Currency,
    string KeyId,
    bool IsLiveGateway
);

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<PaymentService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(decimal amountInInr, string receipt, string currency = "INR")
    {
        var keyId = _configuration["RAZORPAY_KEY_ID"] ?? "rzp_test_TOn4yz0es0iVLQ";
        var keySecret = _configuration["RAZORPAY_KEY_SECRET"] ?? "QLH0R7Wj1lzUDJm3tOAEA0LW";

        var amountInPaise = (long)Math.Round(amountInInr * 100m);

        if (!string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(keySecret))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var basicToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    amount = amountInPaise,
                    currency = currency,
                    receipt = receipt,
                    payment_capture = 1
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var orderId = doc.RootElement.GetProperty("id").GetString() ?? $"order_{Guid.NewGuid():N}";
                    
                    _logger.LogInformation("Created live Razorpay order {OrderId} for receipt {Receipt} (INR {Amount})", orderId, receipt, amountInInr);
                    return new RazorpayOrderResult(orderId, amountInInr, currency, keyId, true);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Razorpay API order creation returned {StatusCode}: {Error}. Falling back to simulation order.", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Razorpay API error. Generating simulated order.");
            }
        }

        // Fallback simulated order
        var simulatedOrderId = $"order_sim_{DateTime.UtcNow:yyyyMMdd}_{RandomNumberGenerator.GetInt32(10000, 99999)}";
        return new RazorpayOrderResult(simulatedOrderId, amountInInr, currency, keyId, false);
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        if (orderId.StartsWith("order_sim_"))
        {
            return true; // Auto-pass simulation payments
        }

        var keySecret = _configuration["RAZORPAY_KEY_SECRET"] ?? "QLH0R7Wj1lzUDJm3tOAEA0LW";
        if (string.IsNullOrWhiteSpace(keySecret))
        {
            return true;
        }

        try
        {
            var payload = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = Convert.ToHexString(hash).ToLowerInvariant();

            return string.Equals(generatedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Razorpay signature.");
            return false;
        }
    }
}
