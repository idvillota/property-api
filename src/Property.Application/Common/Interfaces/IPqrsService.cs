using Property.Application.Common.Models;
using Property.Application.Features.Community.Pqrs;
using Property.Domain.Enums;

namespace Property.Application.Common.Interfaces;

public interface IPqrsService
{
    Task<PagedResult<PqrsCaseDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);

    Task<PqrsCaseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Admin or internal create — also used by portal after scope checks.</summary>
    Task<PqrsCaseDto> CreateAsync(CreatePqrsCaseDto dto, CancellationToken cancellationToken = default);

    Task<PqrsCaseDto?> TransitionStatusAsync(
        Guid id,
        PqrsStatus status,
        DateTime? dueAtUtc = null,
        string? comment = null,
        CancellationToken cancellationToken = default);

    Task<PqrsCommentDto?> AddCommentAsync(
        Guid caseId,
        AddPqrsCommentDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PqrsCaseDto>> ListMineAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Portal detail — excludes internal comments.</summary>
    Task<PqrsCaseDto?> GetMineByIdAsync(
        Guid personId,
        Guid id,
        CancellationToken cancellationToken = default);
}
