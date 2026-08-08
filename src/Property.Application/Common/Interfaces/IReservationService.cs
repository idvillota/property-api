using Property.Application.Common.Models;
using Property.Application.Features.Amenities.Reservations;

namespace Property.Application.Common.Interfaces;

public interface IReservationService
{
    Task<PagedResult<ReservationDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReservationDto> CreateAsync(CreateReservationDto dto, CancellationToken cancellationToken = default);
    Task<ReservationDto?> UpdateAsync(Guid id, UpdateReservationDto dto, CancellationToken cancellationToken = default);
    Task<ReservationDto?> ChangeStatusAsync(Guid id, ChangeReservationStatusDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
