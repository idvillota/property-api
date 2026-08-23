using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Community;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public MaintenanceService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MaintenancePlanDto> CreatePlanAsync(
        CreateMaintenancePlanDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var plan = new MaintenancePlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Name = dto.Name.Trim(),
            Responsible = dto.Responsible?.Trim(),
            Frequency = dto.Frequency,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        plan.Tasks.Add(new MaintenanceTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaintenancePlanId = plan.Id,
            Title = string.IsNullOrWhiteSpace(dto.FirstTaskTitle) ? plan.Name : dto.FirstTaskTitle.Trim(),
            DueDate = dto.FirstDueDate,
            Status = MaintenanceTaskStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.MaintenancePlans.AddAsync(plan, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ListAsync(dto.HorizontalPropertyId, cancellationToken)).First(p => p.Id == plan.Id);
    }

    public async Task<IReadOnlyList<MaintenancePlanDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plans = await _db.MaintenancePlans.AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.HorizontalPropertyId == horizontalPropertyId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new MaintenancePlanDto
        {
            Id = p.Id,
            HorizontalPropertyId = p.HorizontalPropertyId,
            Name = p.Name,
            Responsible = p.Responsible,
            Frequency = p.Frequency,
            Tasks = p.Tasks.OrderBy(t => t.DueDate).Select(t =>
            {
                var overdue = t.Status != MaintenanceTaskStatus.Completed && t.DueDate < today;
                return new MaintenanceTaskDto
                {
                    Id = t.Id,
                    MaintenancePlanId = t.MaintenancePlanId,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    Status = overdue ? MaintenanceTaskStatus.Overdue : t.Status,
                    IsOverdue = overdue,
                };
            }).ToList(),
        }).ToList();
    }

    public async Task<MaintenanceTaskDto> CompleteTaskAsync(
        Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _db.MaintenanceTasks.Include(t => t.MaintenancePlan)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException("Tarea no encontrada.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        task.CompletedAt = today;
        task.Status = MaintenanceTaskStatus.Completed;
        task.UpdatedAtUtc = DateTime.UtcNow;

        var nextDue = NextDue(task.DueDate, task.MaintenancePlan!.Frequency);
        var next = new MaintenanceTask
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            MaintenancePlanId = task.MaintenancePlanId,
            Title = task.Title,
            DueDate = nextDue,
            Status = MaintenanceTaskStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.MaintenanceTasks.AddAsync(next, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new MaintenanceTaskDto
        {
            Id = next.Id,
            MaintenancePlanId = next.MaintenancePlanId,
            Title = next.Title,
            DueDate = next.DueDate,
            Status = next.Status,
            IsOverdue = false,
        };
    }

    private static DateOnly NextDue(DateOnly from, MaintenanceFrequency frequency) => frequency switch
    {
        MaintenanceFrequency.Weekly => from.AddDays(7),
        MaintenanceFrequency.Monthly => from.AddMonths(1),
        MaintenanceFrequency.Quarterly => from.AddMonths(3),
        MaintenanceFrequency.Yearly => from.AddYears(1),
        _ => from.AddMonths(1),
    };

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");
}
