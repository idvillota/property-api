using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Community;

public sealed class PollService : IPollService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public PollService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PollDto> CreateAsync(CreatePollDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Options.Count < 2)
            throw new InvalidOperationException("La encuesta requiere al menos 2 opciones.");

        var tenantId = RequireTenant();
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Status = PollStatus.Draft,
            VoteScope = dto.VoteScope,
            ShowPartialResults = dto.ShowPartialResults,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var order = 0;
        foreach (var opt in dto.Options)
        {
            poll.Options.Add(new PollOption
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PollId = poll.Id,
                Label = opt.Trim(),
                SortOrder = order++,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        await _db.Polls.AddAsync(poll, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(poll.Id, cancellationToken);
    }

    public async Task<PollDto> OpenAsync(Guid pollId, CancellationToken cancellationToken = default)
    {
        var poll = await _db.Polls.FirstOrDefaultAsync(p => p.Id == pollId, cancellationToken)
            ?? throw new InvalidOperationException("Encuesta no encontrada.");
        poll.Status = PollStatus.Open;
        poll.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(pollId, cancellationToken);
    }

    public async Task<PollDto> CloseAsync(Guid pollId, CancellationToken cancellationToken = default)
    {
        var poll = await _db.Polls.FirstOrDefaultAsync(p => p.Id == pollId, cancellationToken)
            ?? throw new InvalidOperationException("Encuesta no encontrada.");
        poll.Status = PollStatus.Closed;
        poll.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(pollId, cancellationToken);
    }

    public async Task<IReadOnlyList<PollDto>> ListAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var ids = await _db.Polls.AsNoTracking()
            .Where(p => p.HorizontalPropertyId == horizontalPropertyId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var list = new List<PollDto>();
        foreach (var id in ids)
            list.Add(await GetMappedAsync(id, cancellationToken));
        return list;
    }

    public async Task<PollDto> CastVoteAsync(CastPollVoteDto dto, CancellationToken cancellationToken = default)
    {
        var poll = await _db.Polls.Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == dto.PollId, cancellationToken)
            ?? throw new InvalidOperationException("Encuesta no encontrada.");
        if (poll.Status != PollStatus.Open)
            throw new InvalidOperationException("La encuesta está cerrada.");
        if (poll.Options.All(o => o.Id != dto.PollOptionId))
            throw new InvalidOperationException("Opción inválida.");

        if (poll.VoteScope == PollVoteScope.PerUnit)
        {
            if (dto.UnitId is null || dto.UnitId == Guid.Empty)
                throw new InvalidOperationException("Se requiere unidad para votar.");
            var exists = await _db.PollVotes.AnyAsync(
                v => v.PollId == dto.PollId && v.UnitId == dto.UnitId, cancellationToken);
            if (exists)
                throw new InvalidOperationException("Ya existe un voto para esta unidad.");
        }
        else
        {
            if (dto.PersonId is null || dto.PersonId == Guid.Empty)
                throw new InvalidOperationException("Se requiere persona para votar.");
            var exists = await _db.PollVotes.AnyAsync(
                v => v.PollId == dto.PollId && v.PersonId == dto.PersonId, cancellationToken);
            if (exists)
                throw new InvalidOperationException("Ya existe un voto para esta persona.");
        }

        await _db.PollVotes.AddAsync(new PollVote
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            PollId = dto.PollId,
            PollOptionId = dto.PollOptionId,
            PersonId = dto.PersonId,
            UnitId = dto.UnitId,
            CreatedAtUtc = DateTime.UtcNow,
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetMappedAsync(dto.PollId, cancellationToken);
    }

    private async Task<PollDto> GetMappedAsync(Guid id, CancellationToken cancellationToken)
    {
        var poll = await _db.Polls.AsNoTracking().Include(p => p.Options)
            .FirstAsync(p => p.Id == id, cancellationToken);
        var counts = await _db.PollVotes.AsNoTracking()
            .Where(v => v.PollId == id)
            .GroupBy(v => v.PollOptionId)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var map = counts.ToDictionary(c => c.OptionId, c => c.Count);
        return new PollDto
        {
            Id = poll.Id,
            HorizontalPropertyId = poll.HorizontalPropertyId,
            Title = poll.Title,
            Description = poll.Description,
            Status = poll.Status,
            VoteScope = poll.VoteScope,
            ShowPartialResults = poll.ShowPartialResults,
            Options = poll.Options.OrderBy(o => o.SortOrder).Select(o => new PollOptionDto
            {
                Id = o.Id,
                Label = o.Label,
                SortOrder = o.SortOrder,
                VoteCount = map.GetValueOrDefault(o.Id),
            }).ToList(),
        };
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");
}
