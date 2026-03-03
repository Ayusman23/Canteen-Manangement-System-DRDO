using Microsoft.AspNetCore.Mvc;
using CanteenApi.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CanteenApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanteenController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly CanteenDbContext _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public CanteenController(IWebHostEnvironment env, CanteenDbContext db)
        {
            _env = env;
            _db = db;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            // Ensure database is created
            _db.Database.EnsureCreated();
        }

        [HttpGet("menu")]
        public IActionResult GetMenu()
        {
            var menuPath = Path.Combine(_env.WebRootPath, "menu.json");
            if (!System.IO.File.Exists(menuPath))
                return NotFound("Menu file not found at " + menuPath);

            var json = System.IO.File.ReadAllText(menuPath);
            try
            {
                var menu = JsonSerializer.Deserialize<List<MenuDay>>(json, _jsonOptions);
                return Ok(menu);
            }
            catch (Exception ex)
            {
                return BadRequest("Error parsing menu file: " + ex.Message);
            }
        }

        [HttpGet("today")]
        public IActionResult GetTodayMenu()
        {
            var menuPath = Path.Combine(_env.WebRootPath, "menu.json");
            if (!System.IO.File.Exists(menuPath))
                return NotFound("Menu file not found at " + menuPath);

            var json = System.IO.File.ReadAllText(menuPath);
            try
            {
                var menu = JsonSerializer.Deserialize<List<MenuDay>>(json, _jsonOptions);

                var today = DateTime.Now.DayOfWeek.ToString();
                var todayMenu = menu.FirstOrDefault(m => m.Day.Equals(today, StringComparison.OrdinalIgnoreCase));
                if (todayMenu == null)
                    return NotFound("Today's menu not found for " + today);

                return Ok(todayMenu);
            }
            catch (Exception ex)
            {
                return BadRequest("Error parsing menu file: " + ex.Message);
            }
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookMeal([FromBody] BookingRequest request)
        {
            if (request == null) return BadRequest("Invalid request.");

            var token = $"DRDO-{new Random().Next(1000, 9999)}";
            
            var booking = new Booking
            {
                Name = request.Name,
                MealType = request.MealType,
                Day = request.Day,
                Token = token,
                BookingDate = DateTime.Now
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            return Ok(new { 
                Token = token, 
                Message = "Booking confirmed and saved!",
                Details = booking 
            });
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings()
        {
            var bookings = await _db.Bookings.OrderByDescending(b => b.BookingDate).ToListAsync();
            return Ok(bookings);
        }
    }

    public class BookingRequest
    {
        public string Name { get; set; }
        public string MealType { get; set; }
        public string Day { get; set; }
    }
}