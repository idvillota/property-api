using Property.Application.Common.Models;
using Property.Application.Features.Organization.Employees;

namespace Property.Application.Common.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
