namespace Property.Application.Features.Organization.TenantSettings;

public sealed class TenantModuleFlagsDto
{
    public bool Portal { get; set; } = true;
    public bool DigitalPayments { get; set; }
    public bool Accounting { get; set; }
    public bool Visits { get; set; }
    public bool Assemblies { get; set; }
}
