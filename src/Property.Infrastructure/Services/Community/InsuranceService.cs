using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Community;

public sealed class InsuranceService : IInsuranceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IOutboxWriter _outbox;

    public InsuranceService(ApplicationDbContext db, ICurrentTenantContext tenant, IOutboxWriter outbox)
    {
        _db = db;
        _tenant = tenant;
        _outbox = outbox;
    }

    public async Task<InsurancePolicyDto> CreateAsync(
        CreateInsurancePolicyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.EndDate is null)
            throw new InvalidOperationException("La fecha de fin de la póliza es obligatoria.");

        var policy = new InsurancePolicy
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            InsurerName = dto.InsurerName.Trim(),
            PolicyNumber = dto.PolicyNumber.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate.Value,
            FileAssetId = dto.FileAssetId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.InsurancePolicies.AddAsync(policy, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<IReadOnlyList<InsurancePolicyDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.InsurancePolicies.AsNoTracking()
            .Where(p => p.HorizontalPropertyId == horizontalPropertyId && p.IsActive)
            .OrderBy(p => p.EndDate)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<int> RunExpiryAlertsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(30);
        var due = await _db.InsurancePolicies
            .Where(p => p.IsActive && p.EndDate >= today && p.EndDate <= horizon)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var policy in due)
        {
            if (policy.LastAlertSentUtc.HasValue &&
                policy.LastAlertSentUtc.Value.Date == DateTime.UtcNow.Date)
                continue;

            await _outbox.EnqueueAsync(
                OutboxMessageTypes.EmailSend,
                new EmailSendPayload
                {
                    ToEmail = "admin@tenant.local",
                    Subject = $"Póliza por vencer: {policy.PolicyNumber}",
                    HtmlBody = $"<p>La póliza {policy.PolicyNumber} ({policy.InsurerName}) vence el {policy.EndDate:yyyy-MM-dd}.</p>",
                },
                cancellationToken);

            policy.LastAlertSentUtc = DateTime.UtcNow;
            policy.UpdatedAtUtc = DateTime.UtcNow;
            sent++;
        }

        if (sent > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static InsurancePolicyDto Map(InsurancePolicy p)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new InsurancePolicyDto
        {
            Id = p.Id,
            HorizontalPropertyId = p.HorizontalPropertyId,
            InsurerName = p.InsurerName,
            PolicyNumber = p.PolicyNumber,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            FileAssetId = p.FileAssetId,
            ExpiringSoon = p.EndDate <= today.AddDays(30) && p.EndDate >= today,
        };
    }
}
