using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IWithholdingTaxService
{
    Task<IReadOnlyList<WithholdingTaxRateDto>> ListRatesAsync(CancellationToken cancellationToken = default);
    Task<WithholdingTaxRateDto> UpsertRateAsync(UpsertWithholdingTaxRateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WithholdingExportRowDto>> ExportAsync(
        Guid horizontalPropertyId, int year, int? month = null, CancellationToken cancellationToken = default);
}
