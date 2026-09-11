using System.ComponentModel.DataAnnotations;
using Canteen.Domain.Enums;

namespace Canteen.Domain.Entities;

public class DailyMenuSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public MealType MealType { get; set; }
    
    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public decimal Price { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentBookingsCount { get; set; }
    public TimeSpan CutoffTime { get; set; } = new TimeSpan(11, 0, 0); // Default 11:00 AM cutoff
    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public bool HasCapacity() => CurrentBookingsCount < MaxCapacity;

    public void ReserveSlot()
    {
        if (!HasCapacity())
        {
            throw new InvalidOperationException($"Meal capacity of {MaxCapacity} for {MealType} on {Date} has been reached.");
        }
        CurrentBookingsCount++;
    }

    public void ReleaseSlot()
    {
        if (CurrentBookingsCount > 0)
        {
            CurrentBookingsCount--;
        }
    }
}
