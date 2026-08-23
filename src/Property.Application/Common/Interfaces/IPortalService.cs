using Property.Application.Features.Billing.PaymentIntents;
using Property.Application.Features.Community.Mural;
using Property.Application.Features.Community.Pqrs;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;
using Property.Application.Features.Reception.Visits;

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

    Task<PortalAccountPaymentDto?> GetAccountPaymentAsync(
        Guid userId,
        Guid paymentId,
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

    Task<IReadOnlyList<MuralPostDto>> ListMuralPostsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MuralPostReadDto> MarkMuralReadAsync(
        Guid userId,
        Guid muralPostId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PqrsCaseDto>> ListMyPqrsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PqrsCaseDto> CreatePqrsAsync(
        Guid userId,
        CreatePortalPqrsDto dto,
        CancellationToken cancellationToken = default);

    Task<PqrsCaseDto?> GetMyPqrsAsync(
        Guid userId,
        Guid id,
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

    Task<IReadOnlyList<VisitLogDto>> ListPendingVisitsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> ApproveVisitAsync(
        Guid userId,
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> RejectVisitAsync(
        Guid userId,
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUnitInScopeAsync(Guid userId, Guid unitId, CancellationToken cancellationToken = default);
}
