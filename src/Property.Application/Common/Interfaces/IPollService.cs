using Property.Application.Features.Community;

namespace Property.Application.Common.Interfaces;

public interface IPollService
{
    Task<PollDto> CreateAsync(CreatePollDto dto, CancellationToken cancellationToken = default);
    Task<PollDto> OpenAsync(Guid pollId, CancellationToken cancellationToken = default);
    Task<PollDto> CloseAsync(Guid pollId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PollDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<PollDto> CastVoteAsync(CastPollVoteDto dto, CancellationToken cancellationToken = default);
}
