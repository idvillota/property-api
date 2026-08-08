using Property.Application.Features.UserPreferences;

namespace Property.Application.Common.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetAsync(CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesDto dto, CancellationToken cancellationToken = default);
}
