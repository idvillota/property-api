namespace Property.Application.Messaging;

public sealed class WhatsAppSendPayload
{
    public string ToPhone { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
}
