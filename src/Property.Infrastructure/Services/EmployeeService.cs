using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Organization.Employees;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _employees;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IRepository<Employee> employees, IUnitOfWork unitOfWork)
    {
        _employees = employees;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<EmployeeDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _employees.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeEmployees(q, query),
            entities => Task.FromResult<IReadOnlyList<EmployeeDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _employees.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Employee
        {
            Id = Guid.NewGuid(),
            TenantUserId = dto.TenantUserId,
            FullName = dto.FullName.Trim(),
            JobTitle = dto.JobTitle?.Trim(),
            HiredOn = dto.HiredOn,
            IsActive = true,
        };
        await _employees.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.TenantUserId = dto.TenantUserId;
        entity.FullName = dto.FullName.Trim();
        entity.JobTitle = dto.JobTitle?.Trim();
        entity.HiredOn = dto.HiredOn;
        entity.IsActive = dto.IsActive;
        _employees.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _employees.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _employees.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static EmployeeDto Map(Employee entity) =>
        new()
        {
            Id = entity.Id,
            TenantUserId = entity.TenantUserId,
            FullName = entity.FullName,
            JobTitle = entity.JobTitle,
            HiredOn = entity.HiredOn,
            IsActive = entity.IsActive,
        };
}
