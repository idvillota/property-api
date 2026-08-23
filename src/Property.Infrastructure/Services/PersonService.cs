using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.People.Persons;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class PersonService : IPersonService
{
    private readonly IRepository<Person> _people;
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public PersonService(IRepository<Person> people, ApplicationDbContext db, IUnitOfWork unitOfWork)
    {
        _people = people;
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<PersonDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _people.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapePeople(q, query),
            entities => Task.FromResult<IReadOnlyList<PersonDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _people.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<PersonDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            WhatsAppOptIn = dto.WhatsAppOptIn,
            IsActive = true,
        };
        await _people.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PersonDto?> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _people.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.DocumentType = dto.DocumentType;
        entity.DocumentNumber = dto.DocumentNumber.Trim();
        entity.FirstName = dto.FirstName.Trim();
        entity.LastName = dto.LastName.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.WhatsAppOptIn = dto.WhatsAppOptIn;
        entity.IsActive = dto.IsActive;
        _people.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _people.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _people.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PersonOverviewDto?> GetOverviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await _people.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
            return null;

        var unitLinks = await (
            from o in _db.UnitOccupancies.AsNoTracking()
            join u in _db.Units.AsNoTracking() on o.UnitId equals u.Id
            join b in _db.Buildings.AsNoTracking() on u.BuildingId equals b.Id into bj
            from b in bj.DefaultIfEmpty()
            join f in _db.Floors.AsNoTracking() on u.FloorId equals f.Id into fj
            from f in fj.DefaultIfEmpty()
            where o.PersonId == id && o.IsActive
            orderby u.Code
            select new
            {
                o.UnitId,
                u.Code,
                u.UnitType,
                o.Role,
                BuildingName = b != null ? b.Name : null,
                FloorName = f != null ? f.Name : null,
                o.IsPrimary,
                o.ReceivesBilling,
            }
        ).ToListAsync(cancellationToken);

        var unitIds = unitLinks.Select(x => x.UnitId).Distinct().ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var openCharges = unitIds.Count == 0
            ? []
            : await _db.FeeCharges.AsNoTracking()
                .Where(c =>
                    unitIds.Contains(c.UnitId) &&
                    c.IsActive &&
                    (c.Status == FeeChargeStatus.Pending || c.Status == FeeChargeStatus.Partial))
                .Select(c => new { c.UnitId, Balance = c.Amount - c.AmountPaid, c.DueDate })
                .ToListAsync(cancellationToken);

        var units = unitLinks.Select(link =>
        {
            var unitCharges = openCharges.Where(c => c.UnitId == link.UnitId).ToList();
            return new PersonUnitLinkDto
            {
                UnitId = link.UnitId,
                UnitCode = link.Code,
                UnitType = link.UnitType,
                Role = link.Role,
                BuildingName = link.BuildingName,
                FloorName = link.FloorName,
                IsPrimary = link.IsPrimary,
                ReceivesBilling = link.ReceivesBilling,
                OpenBalance = unitCharges.Sum(c => c.Balance),
                OverdueBalance = unitCharges.Where(c => c.DueDate < today).Sum(c => c.Balance),
            };
        }).ToList();

        var since = DateTime.UtcNow.AddDays(-90);
        var recentPayments = await (
            from pay in _db.Payments.AsNoTracking()
            join u in _db.Units.AsNoTracking() on pay.UnitId equals u.Id
            where pay.IsActive && pay.PersonId == id
            orderby pay.PaidAt descending
            select new PersonPaymentItemDto
            {
                PaymentId = pay.Id,
                UnitId = pay.UnitId,
                UnitCode = u.Code,
                Amount = pay.Amount,
                PaidAt = pay.PaidAt,
                Method = pay.Method,
            }
        ).Take(12).ToListAsync(cancellationToken);

        var paidLast90 = await _db.Payments.AsNoTracking()
            .Where(p => p.IsActive && p.PersonId == id && p.PaidAt >= since)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var reservations = await (
            from r in _db.Reservations.AsNoTracking()
            join a in _db.CommonAreas.AsNoTracking() on r.CommonAreaId equals a.Id
            join u in _db.Units.AsNoTracking() on r.UnitId equals u.Id
            where r.IsActive && r.PersonId == id
            orderby r.StartsAtUtc descending
            select new PersonReservationItemDto
            {
                ReservationId = r.Id,
                CommonAreaName = a.Name,
                UnitCode = u.Code,
                StartsAtUtc = r.StartsAtUtc,
                EndsAtUtc = r.EndsAtUtc,
                Status = r.Status,
            }
        ).Take(12).ToListAsync(cancellationToken);

        return new PersonOverviewDto
        {
            PersonId = person.Id,
            FullName = $"{person.FirstName} {person.LastName}".Trim(),
            DocumentLabel = $"{person.DocumentType} {person.DocumentNumber}",
            Email = person.Email,
            Phone = person.Phone,
            Units = units,
            PaymentStatus = new PersonPaymentStatusDto
            {
                OpenBalance = units.Sum(u => u.OpenBalance),
                OverdueBalance = units.Sum(u => u.OverdueBalance),
                PaidLast90Days = paidLast90,
                OpenChargeCount = openCharges.Count,
                RecentPayments = recentPayments,
            },
            Reservations = reservations,
        };
    }

    private static PersonDto Map(Person entity) =>
        new()
        {
            Id = entity.Id,
            DocumentType = entity.DocumentType,
            DocumentNumber = entity.DocumentNumber,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            Phone = entity.Phone,
            WhatsAppOptIn = entity.WhatsAppOptIn,
            IsActive = entity.IsActive,
        };
}
