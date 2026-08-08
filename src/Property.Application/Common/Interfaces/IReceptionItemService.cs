using Property.Application.Common.Models;
using Property.Application.Features.Reception.ReceptionItems;

namespace Property.Application.Common.Interfaces;

public interface IReceptionItemService
{
    Task<PagedResult<ReceptionItemDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<ReceptionItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReceptionItemDto> CreateAsync(CreateReceptionItemDto dto, CancellationToken cancellationToken = default);
    Task<ReceptionItemDto?> UpdateAsync(Guid id, UpdateReceptionItemDto dto, CancellationToken cancellationToken = default);
    Task<ReceptionItemDto?> DeliverAsync(Guid id, DeliverReceptionItemDto dto, CancellationToken cancellationToken = default);
    Task<ReceptionItemDto?> ReturnAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReceptionPendingCountDto> GetPendingCountAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
}