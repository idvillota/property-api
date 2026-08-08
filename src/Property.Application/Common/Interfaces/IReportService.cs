using Property.Application.Features.Reports;

namespace Property.Application.Common.Interfaces;

public interface IReportService
{
    Task<ReportOverviewDto> GetOverviewAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);

    Task<CarteraReportDto> GetCarteraAsync(
        Guid horizontalPropertyId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken = default);

    Task<CollectionsReportDto> GetCollectionsAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<OccupancyReportDto> GetOccupancyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);

    Task<CoefficientsReportDto> GetCoefficientsAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default);

    Task<ReservationUsageReportDto> GetReservationUsageAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<ReceptionReportDto> GetReceptionAsync(
        Guid horizontalPropertyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
