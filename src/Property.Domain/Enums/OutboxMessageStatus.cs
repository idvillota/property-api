namespace Property.Domain.Enums;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    DeadLetter = 3,
}
