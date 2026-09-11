using Canteen.Domain.Enums;

namespace Canteen.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MealType Category { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsVegetarian { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DailyMenuSchedule> Schedules { get; set; } = new List<DailyMenuSchedule>();
}
