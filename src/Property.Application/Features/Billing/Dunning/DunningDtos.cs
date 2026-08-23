using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Billing.Dunning;

public sealed class DunningRuleDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsEnabled { get; set; }
    public int CooldownDays { get; set; }
    public string EmailSubjectTemplate { get; set; } = string.Empty;
    public string EmailBodyTemplate { get; set; } = string.Empty;
}

public sealed class UpsertDunningRuleDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Range(1, 3650)]
    public int DaysOverdue { get; set; } = 5;

    public bool IsEnabled { get; set; }

    [Range(1, 365)]
    public int CooldownDays { get; set; } = 7;

    [Required]
    [MaxLength(200)]
    public string EmailSubjectTemplate { get; set; } = "Recordatorio de mora — unidad {{unitCode}}";

    [Required]
    [MaxLength(4000)]
    public string EmailBodyTemplate { get; set; } =
        "<p>Hola {{personName}},</p><p>La unidad <strong>{{unitCode}}</strong> tiene un saldo en mora de {{balance}} COP ({{daysOverdue}} días).</p>";
}

public sealed class DunningSendLogDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitCode { get; set; }
    public Guid DunningRuleId { get; set; }
    public int DaysOverdueSnapshot { get; set; }
    public decimal BalanceSnapshot { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
