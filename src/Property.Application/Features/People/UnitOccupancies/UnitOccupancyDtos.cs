using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.People.UnitOccupancies;

public sealed class UnitOccupancyDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Guid PersonId { get; set; }
    public OccupancyRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesBilling { get; set; }
    public bool IsCommunicationResponsible { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateUnitOccupancyDto
{
    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public OccupancyRole Role { get; set; }

    public bool IsPrimary { get; set; }

    public bool ReceivesBilling { get; set; }

    public bool IsCommunicationResponsible { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}

public sealed class UpdateUnitOccupancyDto
{
    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public OccupancyRole Role { get; set; }

    public bool IsPrimary { get; set; }

    public bool ReceivesBilling { get; set; }

    public bool IsCommunicationResponsible { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}
