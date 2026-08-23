using Property.Domain.Common;

namespace Property.Domain.Entities;

public class InsurancePolicy : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string InsurerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? FileAssetId { get; set; }
    public DateTime? LastAlertSentUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public FileAsset? FileAsset { get; set; }
}
