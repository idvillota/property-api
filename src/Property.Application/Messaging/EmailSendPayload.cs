namespace Property.Application.Messaging;

public sealed class EmailSendPayload
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>Optional FileAsset to attach (BIL-013 receipt).</summary>
    public Guid? AttachmentFileAssetId { get; set; }

    /// <summary>When set, the image is shown in the HTML body via cid: and also attached.</summary>
    public string? InlineContentId { get; set; }
    public string? InlineFileName { get; set; }
    public string? InlineContentType { get; set; }
    public string? InlineContentBase64 { get; set; }
}
