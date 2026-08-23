namespace Property.Application.Common.Options;

public sealed class AiAssistOptions
{
    public const string SectionName = "AiAssist";

    /// <summary>When true, suggestion endpoints throw (controlled failure for tests/demo).</summary>
    public bool Fail { get; set; }
}
