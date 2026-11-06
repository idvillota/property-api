using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class UnitBillingMailDirectory : IUnitBillingMailDirectory
{
    private readonly ApplicationDbContext _db;

    public UnitBillingMailDirectory(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<BillingMailContact>>> ListByUnitAsync(
        IReadOnlyCollection<Guid> unitIds,
        CancellationToken cancellationToken = default)
    {
        if (unitIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<BillingMailContact>>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await (
            from occupancy in _db.UnitOccupancies.AsNoTracking()
            join person in _db.People.AsNoTracking() on occupancy.PersonId equals person.Id
            where unitIds.Contains(occupancy.UnitId)
                && occupancy.IsActive
                && occupancy.ReceivesBilling
                && (occupancy.EndDate == null || occupancy.EndDate >= today)
                && person.IsActive
                && person.Email != null
                && person.Email != ""
            select new
            {
                occupancy.UnitId,
                person.Id,
                person.Email,
                person.FirstName,
                person.LastName,
            }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.UnitId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BillingMailContact>)group
                    .Select(row => new BillingMailContact(
                        row.Id,
                        row.Email!.Trim(),
                        $"{row.FirstName} {row.LastName}".Trim()))
                    .Where(contact => contact.Email.Length > 0)
                    .DistinctBy(contact => contact.Email, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }
}
