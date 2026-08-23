using Property.Domain.Enums;

namespace Property.Application.Features.Portal;

public sealed class PortalUnitCardDto
{
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OccupancyRole OccupancyRole { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class PortalQuickLinkDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public sealed class PortalHomeDto
{
    public Guid PersonId { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public IReadOnlyList<PortalUnitCardDto> Units { get; set; } = [];
    public IReadOnlyList<PortalQuickLinkDto> QuickLinks { get; set; } = [];
}
