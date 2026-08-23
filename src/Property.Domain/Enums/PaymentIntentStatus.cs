namespace Property.Domain.Enums;

public enum PaymentIntentStatus
{
    Pending = 0,
    RequiresAction = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
}
