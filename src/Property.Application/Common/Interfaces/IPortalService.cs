using Property.Application.Features.Billing.PaymentIntents;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;

namespace Property.Application.Common.Interfaces;

public interface IPortalService
{
    Task<PortalMeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PortalHomeDto> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PortalAccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PortalAccountUnitDetailDto> GetAccountUnitAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortalCommonAreaDto>> ListReservationAreasAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortalReservationSlotDto>> ListOccupiedSlotsAsync(
        Guid userId,
        Guid commonAreaId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortalReservationDto>> ListMyReservationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PortalReservationDto> CreateReservationAsync(
        Guid userId,
        CreatePortalReservationDto dto,
        CancellationToken cancellationToken = default);

    Task CancelReservationAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<PortalCheckoutDto> StartPaymentAsync(
        Guid userId,
        StartPortalPaymentDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto?> GetPaymentIntentAsync(
        Guid userId,
        Guid intentId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> ConfirmSandboxPaymentAsync(
        Guid userId,
        Guid intentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUnitInScopeAsync(Guid userId, Guid unitId, CancellationToken cancellationToken = default);
}
