namespace Property.Application.Messaging;

public sealed class EmailSendPayload
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>Optional FileAsset to attach (BIL-013 receipt).</summary>
    public Guid? AttachmentFileAssetId { get; set; }
}
