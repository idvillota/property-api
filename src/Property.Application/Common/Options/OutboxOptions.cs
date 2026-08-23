namespace Property.Application.Common.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int DefaultMaxAttempts { get; set; } = 5;

    /// <summary>Base delay in seconds; backoff is base * 2^(attempt-1).</summary>
    public int RetryBaseDelaySeconds { get; set; } = 5;
}
