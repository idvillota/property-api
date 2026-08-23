namespace Property.Application.Features.AiAssist;

public sealed class AiAssistDraftDto
{
    public string Draft { get; set; } = string.Empty;
}

public sealed class SuggestDunningTextRequestDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string? UnitCode { get; set; }
    public decimal? Balance { get; set; }
    public int? DaysOverdue { get; set; }
}
