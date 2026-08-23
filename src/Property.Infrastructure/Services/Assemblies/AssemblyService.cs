using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Assemblies;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Assemblies;

public sealed class AssemblyService : IAssemblyService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;
    private readonly IOutboxWriter _outbox;
    private readonly IFileAssetService _files;

    public AssemblyService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IOutboxWriter outbox,
        IFileAssetService files)
    {
        _db = db;
        _tenant = tenant;
        _outbox = outbox;
        _files = files;
    }

    public async Task<AssemblyDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ScheduledAtUtc is null)
            throw new InvalidOperationException("La fecha de la asamblea es obligatoria.");

        var tenantId = RequireTenant();
        var entity = new Assembly
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Title = dto.Title.Trim(),
            ScheduledAtUtc = Normalize(dto.ScheduledAtUtc.Value),
            Location = dto.Location?.Trim(),
            QuorumPercentRequired = dto.QuorumPercentRequired,
            Status = AssemblyStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
        };
        foreach (var item in dto.AgendaItems)
        {
            entity.AgendaItems.Add(new AgendaItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssemblyId = entity.Id,
                SortOrder = item.SortOrder,
                Title = item.Title.Trim(),
                Description = item.Description?.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await _db.Assemblies.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<AssemblyDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Assemblies.AsNoTracking()
            .Include(a => a.AgendaItems)
            .Where(a => a.HorizontalPropertyId == horizontalPropertyId)
            .OrderByDescending(a => a.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AssemblyDto>> ListPublishedAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Assemblies.AsNoTracking()
            .Include(a => a.AgendaItems)
            .Where(a =>
                a.HorizontalPropertyId == horizontalPropertyId &&
                a.Status == AssemblyStatus.Published)
            .OrderByDescending(a => a.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AssemblyMotionDto>> ListOpenMotionsAsync(
        Guid assemblyId, CancellationToken cancellationToken = default)
    {
        return await _db.AssemblyMotions.AsNoTracking()
            .Where(m => m.AssemblyId == assemblyId && m.Status == AssemblyMotionStatus.Open)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new AssemblyMotionDto
            {
                Id = m.Id,
                AssemblyId = m.AssemblyId,
                Title = m.Title,
                Description = m.Description,
                VoteMode = m.VoteMode,
                Status = m.Status,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AssemblyDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Assemblies.AsNoTracking()
            .Include(a => a.AgendaItems)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<AssemblyDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Assemblies.Include(a => a.AgendaItems)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Asamblea no encontrada.");
        if (entity.Status != AssemblyStatus.Draft)
            throw new InvalidOperationException("Solo se puede publicar una asamblea en borrador.");

        entity.Status = AssemblyStatus.Published;
        entity.PublishedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var ownerEmails = await (
            from o in _db.UnitOccupancies.AsNoTracking()
            join u in _db.Units.AsNoTracking() on o.UnitId equals u.Id
            join p in _db.People.AsNoTracking() on o.PersonId equals p.Id
            where u.HorizontalPropertyId == entity.HorizontalPropertyId
                  && o.Role == OccupancyRole.Owner
                  && o.IsActive
                  && p.Email != null
                  && p.Email != ""
            select p.Email).Distinct().ToListAsync(cancellationToken);

        foreach (var email in ownerEmails)
        {
            await _outbox.EnqueueAsync(
                OutboxMessageTypes.EmailSend,
                new EmailSendPayload
                {
                    ToEmail = email!,
                    Subject = $"Convocatoria: {entity.Title}",
                    HtmlBody = $"<p>Se convocó la asamblea <strong>{entity.Title}</strong> para {entity.ScheduledAtUtc:yyyy-MM-dd HH:mm} UTC.</p>",
                },
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<QuorumStatusDto> RegisterAttendanceAsync(
        RegisterAttendanceDto dto, CancellationToken cancellationToken = default)
    {
        var assembly = await _db.Assemblies.FirstOrDefaultAsync(a => a.Id == dto.AssemblyId, cancellationToken)
            ?? throw new InvalidOperationException("Asamblea no encontrada.");
        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unidad no encontrada.");

        var existing = await _db.AssemblyAttendances
            .FirstOrDefaultAsync(a => a.AssemblyId == dto.AssemblyId && a.UnitId == dto.UnitId, cancellationToken);
        if (existing is null)
        {
            await _db.AssemblyAttendances.AddAsync(new AssemblyAttendance
            {
                Id = Guid.NewGuid(),
                TenantId = RequireTenant(),
                AssemblyId = dto.AssemblyId,
                UnitId = dto.UnitId,
                PersonId = dto.PersonId,
                CoefficientSnapshot = unit.Coefficient,
                Present = true,
                CreatedAtUtc = DateTime.UtcNow,
            }, cancellationToken);
        }
        else
        {
            existing.Present = true;
            existing.PersonId = dto.PersonId;
            existing.CoefficientSnapshot = unit.Coefficient;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetQuorumAsync(dto.AssemblyId, cancellationToken);
    }

    public async Task<QuorumStatusDto> RegisterProxyAsync(
        RegisterProxyDto dto, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.GrantorUnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unidad otorgante no encontrada.");

        await _db.AssemblyProxies.AddAsync(new AssemblyProxy
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            AssemblyId = dto.AssemblyId,
            GrantorUnitId = dto.GrantorUnitId,
            ProxyPersonId = dto.ProxyPersonId,
            CoefficientSnapshot = unit.Coefficient,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetQuorumAsync(dto.AssemblyId, cancellationToken);
    }

    public async Task<QuorumStatusDto> GetQuorumAsync(Guid assemblyId, CancellationToken cancellationToken = default)
    {
        var assembly = await _db.Assemblies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assemblyId, cancellationToken)
            ?? throw new InvalidOperationException("Asamblea no encontrada.");

        var total = await _db.Units.AsNoTracking()
            .Where(u => u.HorizontalPropertyId == assembly.HorizontalPropertyId && u.IsActive && u.IsBillable)
            .SumAsync(u => u.Coefficient, cancellationToken);

        var present = await _db.AssemblyAttendances.AsNoTracking()
            .Where(a => a.AssemblyId == assemblyId && a.Present)
            .SumAsync(a => a.CoefficientSnapshot, cancellationToken);

        var attendedUnitIds = await _db.AssemblyAttendances.AsNoTracking()
            .Where(a => a.AssemblyId == assemblyId && a.Present)
            .Select(a => a.UnitId)
            .ToListAsync(cancellationToken);

        var proxy = await _db.AssemblyProxies.AsNoTracking()
            .Where(p => p.AssemblyId == assemblyId && p.IsActive && !attendedUnitIds.Contains(p.GrantorUnitId))
            .SumAsync(p => p.CoefficientSnapshot, cancellationToken);

        var represented = present + proxy;
        var pct = total <= 0 ? 0 : Math.Round(represented / total * 100m, 2);
        return new QuorumStatusDto
        {
            AssemblyId = assemblyId,
            TotalCoefficient = total,
            PresentCoefficient = present,
            ProxyCoefficient = proxy,
            RepresentedCoefficient = represented,
            QuorumPercentRequired = assembly.QuorumPercentRequired,
            AchievedPercent = pct,
            HasQuorum = pct >= assembly.QuorumPercentRequired,
        };
    }

    public async Task<AssemblyMotionDto> CreateMotionAsync(
        CreateMotionDto dto, CancellationToken cancellationToken = default)
    {
        var motion = new AssemblyMotion
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            AssemblyId = dto.AssemblyId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            VoteMode = dto.VoteMode,
            Status = AssemblyMotionStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.AssemblyMotions.AddAsync(motion, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new AssemblyMotionDto
        {
            Id = motion.Id,
            AssemblyId = motion.AssemblyId,
            Title = motion.Title,
            Description = motion.Description,
            VoteMode = motion.VoteMode,
            Status = motion.Status,
        };
    }

    public async Task<MotionResultDto> CastVoteAsync(CastVoteDto dto, CancellationToken cancellationToken = default)
    {
        var motion = await _db.AssemblyMotions.FirstOrDefaultAsync(m => m.Id == dto.MotionId, cancellationToken)
            ?? throw new InvalidOperationException("Moción no encontrada.");
        if (motion.Status != AssemblyMotionStatus.Open)
            throw new InvalidOperationException("La moción está cerrada.");

        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unidad no encontrada.");

        var weight = motion.VoteMode == AssemblyVoteMode.Coefficient ? unit.Coefficient : 1m;
        var existing = await _db.AssemblyVotes
            .FirstOrDefaultAsync(v => v.AssemblyMotionId == dto.MotionId && v.UnitId == dto.UnitId, cancellationToken);
        if (existing is null)
        {
            await _db.AssemblyVotes.AddAsync(new AssemblyVote
            {
                Id = Guid.NewGuid(),
                TenantId = RequireTenant(),
                AssemblyMotionId = dto.MotionId,
                UnitId = dto.UnitId,
                PersonId = dto.PersonId,
                Choice = dto.Choice,
                Weight = weight,
                CreatedAtUtc = DateTime.UtcNow,
            }, cancellationToken);
        }
        else
        {
            existing.Choice = dto.Choice;
            existing.Weight = weight;
            existing.PersonId = dto.PersonId;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ResultAsync(motion.Id, cancellationToken);
    }

    public async Task<MotionResultDto> CloseMotionAsync(Guid motionId, CancellationToken cancellationToken = default)
    {
        var motion = await _db.AssemblyMotions.FirstOrDefaultAsync(m => m.Id == motionId, cancellationToken)
            ?? throw new InvalidOperationException("Moción no encontrada.");
        motion.Status = AssemblyMotionStatus.Closed;
        motion.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await ResultAsync(motionId, cancellationToken);
    }

    public async Task<IReadOnlyList<MotionResultDto>> ListMotionResultsAsync(
        Guid assemblyId, CancellationToken cancellationToken = default)
    {
        var ids = await _db.AssemblyMotions.AsNoTracking()
            .Where(m => m.AssemblyId == assemblyId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        var results = new List<MotionResultDto>();
        foreach (var id in ids)
            results.Add(await ResultAsync(id, cancellationToken));
        return results;
    }

    public async Task<AssemblyDto> GenerateMinutesAsync(Guid assemblyId, CancellationToken cancellationToken = default)
    {
        var assembly = await _db.Assemblies.Include(a => a.AgendaItems)
            .FirstOrDefaultAsync(a => a.Id == assemblyId, cancellationToken)
            ?? throw new InvalidOperationException("Asamblea no encontrada.");

        var results = await ListMotionResultsAsync(assemblyId, cancellationToken);
        var quorum = await GetQuorumAsync(assemblyId, cancellationToken);
        var lines = new List<string>
        {
            $"Quorum: {(quorum.HasQuorum ? "SI" : "NO")} ({quorum.AchievedPercent}%)",
            $"Asistencia coef: {quorum.RepresentedCoefficient}/{quorum.TotalCoefficient}",
        };
        foreach (var r in results)
            lines.Add($"Mocion: {r.Title} SI={r.YesWeight} NO={r.NoWeight} ABS={r.AbstainWeight}");

        var pdf = MinutesPdfBuilder.Build(assembly.Title, assembly.ScheduledAtUtc, lines);
        await using var ms = new MemoryStream(pdf);
        var asset = await _files.UploadAsync(
            ms, $"acta-{assembly.Id:N}.pdf", "application/pdf", pdf.Length, "assembly-minutes", cancellationToken);
        assembly.MinutesFileAssetId = asset.Id;
        assembly.Status = AssemblyStatus.Closed;
        assembly.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(assembly);
    }

    private async Task<MotionResultDto> ResultAsync(Guid motionId, CancellationToken cancellationToken)
    {
        var motion = await _db.AssemblyMotions.AsNoTracking().FirstAsync(m => m.Id == motionId, cancellationToken);
        var votes = await _db.AssemblyVotes.AsNoTracking()
            .Where(v => v.AssemblyMotionId == motionId)
            .ToListAsync(cancellationToken);
        return new MotionResultDto
        {
            MotionId = motion.Id,
            Title = motion.Title,
            Status = motion.Status,
            VoteMode = motion.VoteMode,
            YesWeight = votes.Where(v => v.Choice == AssemblyVoteChoice.Yes).Sum(v => v.Weight),
            NoWeight = votes.Where(v => v.Choice == AssemblyVoteChoice.No).Sum(v => v.Weight),
            AbstainWeight = votes.Where(v => v.Choice == AssemblyVoteChoice.Abstain).Sum(v => v.Weight),
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static DateTime Normalize(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

    private static AssemblyDto Map(Assembly a) => new()
    {
        Id = a.Id,
        HorizontalPropertyId = a.HorizontalPropertyId,
        Title = a.Title,
        ScheduledAtUtc = a.ScheduledAtUtc,
        Location = a.Location,
        Status = a.Status,
        PublishedAtUtc = a.PublishedAtUtc,
        MinutesFileAssetId = a.MinutesFileAssetId,
        QuorumPercentRequired = a.QuorumPercentRequired,
        AgendaItems = a.AgendaItems.OrderBy(i => i.SortOrder).Select(i => new AgendaItemDto
        {
            Id = i.Id,
            SortOrder = i.SortOrder,
            Title = i.Title,
            Description = i.Description,
        }).ToList(),
    };
}
