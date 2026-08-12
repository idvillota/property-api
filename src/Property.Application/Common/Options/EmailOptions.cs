namespace Property.Application.Common.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>When false, emails are logged only (useful for local/dev).</summary>
    public bool Enabled { get; set; }

    public string FromAddress { get; set; } = "noreply@property.local";
    public string FromName { get; set; } = "Portería";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
