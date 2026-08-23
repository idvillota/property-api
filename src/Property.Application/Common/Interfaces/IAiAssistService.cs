using Property.Application.Features.AiAssist;

namespace Property.Application.Common.Interfaces;

public interface IAiAssistService
{
    Task<string> SuggestPqrsReplyAsync(Guid pqrsId, CancellationToken cancellationToken = default);

    Task<string> SuggestDunningTextAsync(
        SuggestDunningTextRequestDto request,
        CancellationToken cancellationToken = default);
}
