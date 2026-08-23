using Property.Application.Features.Parking;

namespace Property.Application.Common.Interfaces;

public interface IVisitorParkingService
{
    Task<VisitorParkingSettingsDto> UpsertSettingsAsync(UpsertVisitorParkingSettingsDto dto, CancellationToken cancellationToken = default);
    Task<VisitorParkingSettingsDto?> GetSettingsAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<ParkingShiftDto> OpenShiftAsync(OpenParkingShiftDto dto, CancellationToken cancellationToken = default);
    Task<ParkingShiftDto> CloseShiftAsync(CloseParkingShiftDto dto, CancellationToken cancellationToken = default);
    Task<ParkingShiftDto?> GetOpenShiftAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<ParkingTicketDto> IssueTicketAsync(IssueParkingTicketDto dto, CancellationToken cancellationToken = default);
    Task<ParkingTicketDto> ExitTicketAsync(ExitParkingTicketDto dto, CancellationToken cancellationToken = default);
    decimal CalculateFee(decimal hourlyRate, DateTime enteredAtUtc, DateTime exitedAtUtc, decimal? dailyCap);
}
