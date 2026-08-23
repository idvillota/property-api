namespace Property.Application.Common.Options;

public sealed class DunningOptions
{
    public const string SectionName = "Dunning";

    /// <summary>How often the worker evaluates mora rules (seconds). Default 1 hour.</summary>
    public int PollIntervalSeconds { get; set; } = 3600;
}
