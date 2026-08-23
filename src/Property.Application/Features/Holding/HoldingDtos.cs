namespace Property.Application.Features.Holding;

public sealed class HoldingPropertySummaryDto
{
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpenBalance { get; set; }
    public decimal OverdueBalance { get; set; }
    public int OccupiedUnits { get; set; }
    public int TotalUnits { get; set; }
}

public sealed class HoldingSummaryDto
{
    public IReadOnlyList<HoldingPropertySummaryDto> Properties { get; set; } = [];
}
