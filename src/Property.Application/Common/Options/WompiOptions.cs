namespace Property.Application.Common.Options;

public sealed class WompiOptions
{
    public const string SectionName = "Payments:Wompi";

    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string EventsSecret { get; set; } = string.Empty;

    /// <summary>sandbox | production</summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// When true (default in sandbox without keys), checkout uses in-app confirm instead of live Wompi.
    /// </summary>
    public bool UseMockCheckout { get; set; } = true;

    /// <summary>Frontend origin for redirect after checkout (e.g. http://localhost:5173).</summary>
    public string PortalPublicBaseUrl { get; set; } = "http://localhost:5173";
}
