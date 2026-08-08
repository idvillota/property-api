using Property.Application.Common.Models;
using Property.Application.Features.People.Persons;

namespace Property.Application.Common.Interfaces;

public interface IPersonService
{
    Task<PagedResult<PersonDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PersonDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default);
    Task<PersonDto?> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
