using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class CommonArea : NamedTenantEntity
{
    public Guid HorizontalPropertyId { get; set; }
    public CommonAreaType AreaType { get; set; }
    public int Capacity { get; set; } = 1;
    public bool RequiresDeposit { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? RulesNotes { get; set; }
    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
