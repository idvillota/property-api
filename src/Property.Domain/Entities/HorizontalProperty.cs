using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class HorizontalProperty : NamedTenantEntity
{
    public string? TradeName { get; set; }
    public string? LegalName { get; set; }
    public string? Nit { get; set; }
    public string? TaxRegime { get; set; }
    public string? LegalRepresentative { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Colombia";
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public PropertyUseType PropertyUseType { get; set; }
    public string? MatriculaInmobiliaria { get; set; }
    public string? RegulationNotes { get; set; }
    public string? CoefficientBasisNotes { get; set; }

    public Neighborhood? Neighborhood { get; set; }
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<CommonArea> CommonAreas { get; set; } = new List<CommonArea>();
}
