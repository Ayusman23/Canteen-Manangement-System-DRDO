namespace Canteen.Domain.Enums;

public enum MealType
{
    Normal = 1,
    Special = 2,
    Item = 3
}

public enum BookingStatus
{
    Confirmed = 1,
    Dispensed = 2,
    Cancelled = 3
}

public enum UserRole
{
    Employee = 1,
    KitchenOperator = 2,
    CanteenManager = 3
}
