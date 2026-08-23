using Property.Application.Features.Assemblies;

namespace Property.Application.Common.Interfaces;

public interface IAssemblyService
{
    Task<AssemblyDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssemblyDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssemblyDto>> ListPublishedAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssemblyMotionDto>> ListOpenMotionsAsync(Guid assemblyId, CancellationToken cancellationToken = default);
    Task<AssemblyDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssemblyDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuorumStatusDto> RegisterAttendanceAsync(RegisterAttendanceDto dto, CancellationToken cancellationToken = default);
    Task<QuorumStatusDto> RegisterProxyAsync(RegisterProxyDto dto, CancellationToken cancellationToken = default);
    Task<QuorumStatusDto> GetQuorumAsync(Guid assemblyId, CancellationToken cancellationToken = default);
    Task<AssemblyMotionDto> CreateMotionAsync(CreateMotionDto dto, CancellationToken cancellationToken = default);
    Task<MotionResultDto> CastVoteAsync(CastVoteDto dto, CancellationToken cancellationToken = default);
    Task<MotionResultDto> CloseMotionAsync(Guid motionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MotionResultDto>> ListMotionResultsAsync(Guid assemblyId, CancellationToken cancellationToken = default);
    Task<AssemblyDto> GenerateMinutesAsync(Guid assemblyId, CancellationToken cancellationToken = default);
}
