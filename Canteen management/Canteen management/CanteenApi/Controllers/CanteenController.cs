using Microsoft.AspNetCore.Mvc;
using CanteenApi.Models;
using System.Text.Json;

namespace CanteenApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanteenController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public CanteenController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("menu")]
        public IActionResult GetMenu()
        {
            var menuPath = Path.Combine(_env.WebRootPath, "menu.json");
            if (!System.IO.File.Exists(menuPath))
                return NotFound("Menu file not found.");

            var json = System.IO.File.ReadAllText(menuPath);
            var menu = JsonSerializer.Deserialize<List<MenuDay>>(json);
            return Ok(menu);
        }

        [HttpGet("today")]
        public IActionResult GetTodayMenu()
        {
            var menuPath = Path.Combine(_env.WebRootPath, "menu.json");
            if (!System.IO.File.Exists(menuPath))
                return NotFound("Menu file not found.");

            var json = System.IO.File.ReadAllText(menuPath);
            var menu = JsonSerializer.Deserialize<List<MenuDay>>(json);

            var today = DateTime.Now.DayOfWeek.ToString();
            var todayMenu = menu.FirstOrDefault(m => m.Day.Equals(today, StringComparison.OrdinalIgnoreCase));
            if (todayMenu == null)
                return NotFound("Today's menu not found.");

            return Ok(todayMenu);
        }

        [HttpPost("book")]
        public IActionResult BookMeal([FromBody] BookingRequest request)
        {
            var token = $"S-{new Random().Next(10, 99)}";
            return Ok(new { Token = token });
        }
    }

    public class BookingRequest
    {
        public string Name { get; set; }
        public string MealType { get; set; }
        public string Day { get; set; }
    }
} 