using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IAccountsPayableService
{
    Task<VendorDto> CreateVendorAsync(CreateVendorDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorDto>> ListVendorsAsync(CancellationToken cancellationToken = default);
    Task<ApInvoiceDto> CreateInvoiceAsync(CreateApInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApInvoiceDto>> ListInvoicesAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<ApPaymentDto> CreatePaymentAsync(CreateApPaymentDto dto, CancellationToken cancellationToken = default);
}
