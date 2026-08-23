using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>BIL-012 — overdue email reminder rule per property.</summary>
public class DunningRule : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }

    /// <summary>Send when the oldest open charge is at least this many days past due.</summary>
    public int DaysOverdue { get; set; } = 5;

    public bool IsEnabled { get; set; }

    /// <summary>Minimum days between reminder emails for the same unit + rule.</summary>
    public int CooldownDays { get; set; } = 7;

    public string EmailSubjectTemplate { get; set; } = "Recordatorio de mora — unidad {{unitCode}}";

    public string EmailBodyTemplate { get; set; } =
        "<p>Hola {{personName}},</p><p>La unidad <strong>{{unitCode}}</strong> tiene un saldo en mora de {{balance}} COP ({{daysOverdue}} días).</p><p>Por favor regularice su cuota.</p>";

    public HorizontalProperty? HorizontalProperty { get; set; }
}
