using Canteen.Domain.Entities;
using Canteen.Domain.Enums;
using Canteen.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Canteen.Infrastructure.Persistence;

public static class CanteenDataSeeder
{
    public static async Task SeedAsync(
        CanteenDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        try
        {
            // Ensure schema is created
            await db.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            var roles = new[] { "CanteenManager", "KitchenOperator", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole(role));
                    logger.LogInformation("Seeded Role: {Role}", role);
                }
            }

            // 2. Seed Pre-configured Users
            await SeedUserAsync(userManager, "manager@drdo.gov.in", "Manager@DRDO2026!", "Col. Rajesh Verma", "DRDO-MGR-01", "Canteen Logistics", "CanteenManager", logger);
            await SeedUserAsync(userManager, "kitchen@drdo.gov.in", "Kitchen@DRDO2026!", "Chef Amit Kumar", "DRDO-KIT-07", "Culinary Operations", "KitchenOperator", logger);
            await SeedUserAsync(userManager, "employee@drdo.gov.in", "Employee@DRDO2026!", "Dr. Ayusman Mohanty", "DRDO-SCI-42", "Avionics Research", "Employee", logger);

            // 3. Seed Menu Items & Schedules if not existing
            if (!await db.MenuItems.AnyAsync())
            {
                logger.LogInformation("Seeding Menu Items and Dynamic Weekly Schedules...");

                var menuData = new[]
                {
                    (DayOfWeek.Monday, "Rice, Dal, Sabzi", 30m, "Paneer Curry Thali", 50m, "Samosa (2 pcs)", 15m),
                    (DayOfWeek.Tuesday, "Roti, Chana, Rice", 30m, "Chicken Curry Set", 50m, "Veg Roll", 25m),
                    (DayOfWeek.Wednesday, "Rice, Dal, Aloo", 30m, "Fish Curry Thali", 50m, "Rasgulla (2 pcs)", 20m),
                    (DayOfWeek.Thursday, "Pulao, Raita, Dal", 30m, "Egg Curry Meal", 50m, "Bread Pakora", 15m),
                    (DayOfWeek.Friday, "Roti, Sabzi, Dal", 30m, "Paneer Butter Set", 50m, "Gulab Jamun (2)", 20m),
                    (DayOfWeek.Saturday, "Rice, Chole, Salad", 30m, "Mutton Curry Thali", 50m, "Kachori (2 pcs)", 20m),
                    (DayOfWeek.Sunday, "Khichdi, Chutney", 30m, "Veg Biryani Meal", 50m, "Dahi Vada", 25m),
                };

                // Find Monday of current week
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var currentMonday = today.AddDays(-1 * diff);

                foreach (var (day, normalMeal, normalPrice, specialMeal, specialPrice, addon, addonPrice) in menuData)
                {
                    // Create Menu Items
                    var normalItem = new MenuItem { Name = normalMeal, Description = "Standard nutritious DRDO mess meal", Category = MealType.Normal, BasePrice = normalPrice, IsVegetarian = true };
                    var specialItem = new MenuItem { Name = specialMeal, Description = "Chef's gourmet special preparation", Category = MealType.Special, BasePrice = specialPrice, IsVegetarian = !specialMeal.Contains("Chicken") && !specialMeal.Contains("Fish") && !specialMeal.Contains("Mutton") && !specialMeal.Contains("Egg") };
                    var addonItem = new MenuItem { Name = addon, Description = "Freshly prepared snack or dessert", Category = MealType.Item, BasePrice = addonPrice, IsVegetarian = true };

                    db.MenuItems.AddRange(normalItem, specialItem, addonItem);

                    // Compute date for current week
                    int dayOffset = ((int)day - (int)DayOfWeek.Monday + 7) % 7;
                    var scheduledDate = currentMonday.AddDays(dayOffset);

                    // Create Daily Schedules
                    db.DailyMenuSchedules.Add(new DailyMenuSchedule
                    {
                        Date = scheduledDate,
                        DayOfWeek = day,
                        MealType = MealType.Normal,
                        MenuItem = normalItem,
                        Price = normalPrice,
                        MaxCapacity = 150,
                        CurrentBookingsCount = 0,
                        CutoffTime = new TimeSpan(11, 30, 0)
                    });

                    db.DailyMenuSchedules.Add(new DailyMenuSchedule
                    {
                        Date = scheduledDate,
                        DayOfWeek = day,
                        MealType = MealType.Special,
                        MenuItem = specialItem,
                        Price = specialPrice,
                        MaxCapacity = 75,
                        CurrentBookingsCount = 0,
                        CutoffTime = new TimeSpan(11, 30, 0)
                    });

                    db.DailyMenuSchedules.Add(new DailyMenuSchedule
                    {
                        Date = scheduledDate,
                        DayOfWeek = day,
                        MealType = MealType.Item,
                        MenuItem = addonItem,
                        Price = addonPrice,
                        MaxCapacity = 100,
                        CurrentBookingsCount = 0,
                        CutoffTime = new TimeSpan(16, 0, 0)
                    });
                }

                await db.SaveChangesAsync();
                logger.LogInformation("Database seeded with 21 dynamic menu schedules.");
            }

            // 4. Seed initial sample booking if empty so analytics & kiosk have instant live data
            if (!await db.Bookings.AnyAsync())
            {
                var todaySchedule = await db.DailyMenuSchedules.Include(s => s.MenuItem).FirstOrDefaultAsync();
                if (todaySchedule != null)
                {
                    var token = "DRDO-20260911-7788";
                    var booking = new Booking
                    {
                        BookingReference = token,
                        UserName = "Dr. Ayusman Mohanty",
                        UserEmail = "employee@drdo.gov.in",
                        EmployeeCode = "DRDO-SCI-42",
                        ScheduleId = todaySchedule.Id,
                        MealType = todaySchedule.MealType,
                        MealName = todaySchedule.MenuItem?.Name ?? "Normal Thali",
                        Price = todaySchedule.Price,
                        ScheduledMealDate = todaySchedule.Date,
                        Status = BookingStatus.Confirmed,
                        BookingDate = DateTime.UtcNow.AddHours(-1),
                        QrCodeHash = "INITIAL_HASH"
                    };
                    todaySchedule.ReserveSlot();
                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
        }
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string employeeCode,
        string department,
        string role,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                EmployeeCode = employeeCode,
                Department = department,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Seeded user: {Email} ({Role})", email, role);
            }
            else
            {
                logger.LogWarning("Failed to seed user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
