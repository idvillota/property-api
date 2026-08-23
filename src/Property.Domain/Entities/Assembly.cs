using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class Assembly : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public string? Location { get; set; }
    public AssemblyStatus Status { get; set; } = AssemblyStatus.Draft;
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? MinutesFileAssetId { get; set; }
    public decimal QuorumPercentRequired { get; set; } = 50m;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public FileAsset? MinutesFileAsset { get; set; }
    public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
    public ICollection<AssemblyAttendance> Attendances { get; set; } = new List<AssemblyAttendance>();
    public ICollection<AssemblyProxy> Proxies { get; set; } = new List<AssemblyProxy>();
    public ICollection<AssemblyMotion> Motions { get; set; } = new List<AssemblyMotion>();
}
