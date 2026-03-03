using Microsoft.EntityFrameworkCore;

namespace CanteenApi.Models
{
    public class CanteenDbContext : DbContext
    {
        public CanteenDbContext(DbContextOptions<CanteenDbContext> options) : base(options) { }

        public DbSet<Booking> Bookings { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MealType { get; set; }
        public string Day { get; set; }
        public string Token { get; set; }
        public DateTime BookingDate { get; set; }
    }
}
