using System.Globalization;
using System.Net;

namespace Property.Application.Features.Billing.BreB;

public static class AdminFeeNoticeMessage
{
    public const string QrContentId = "cuota-qr";

    public static (string Subject, string HtmlBody) Compose(
        string recipientName,
        string propertyName,
        string unitCode,
        string periodKey,
        decimal amount,
        decimal coefficient,
        DateOnly dueDate,
        string reference)
    {
        var culture = CultureInfo.GetCultureInfo("es-CO");
        var money = amount.ToString("N2", culture);
        var coef = coefficient.ToString("0.##", CultureInfo.InvariantCulture);
        var due = dueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var who = string.IsNullOrWhiteSpace(recipientName) ? "residente" : recipientName.Trim();

        var subject = $"Cuota de administración {periodKey} — unidad {unitCode}";
        var html = $"""
            <p>Hola {Encode(who)},</p>
            <p>La cuota de administración de <strong>{Encode(propertyName)}</strong> para la unidad <strong>{Encode(unitCode)}</strong> del periodo <strong>{Encode(periodKey)}</strong> ya está liquidada.</p>
            <ul>
            <li>Valor: {Encode(money)}</li>
            <li>Vencimiento: {Encode(due)}</li>
            <li>Coeficiente: {Encode(coef)}</li>
            <li>Referencia: {Encode(reference)}</li>
            </ul>
            <p>Paga escaneando este código Bre-B desde tu banco. El valor ya va dentro del código.</p>
            <p><img src="cid:{QrContentId}" alt="QR Bre-B para la cuota de administración" /></p>
            <p>Este correo no registra el pago. La copropiedad lo aplica cuando la transferencia llegue.</p>
            """;
        return (subject, html);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
