using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class UserPersonLinkService : IUserPersonLinkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public UserPersonLinkService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext db,
        ICurrentTenantContext tenant)
    {
        _unitOfWork = unitOfWork;
        _db = db;
        _tenant = tenant;
    }

    public async Task<UserPersonLinkDto?> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonId == personId && x.IsActive, cancellationToken);
        if (link is null) return null;
        return await MapAsync(link, cancellationToken);
    }

    public async Task<UserPersonLinkDto?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (link is null) return null;
        return await MapAsync(link, cancellationToken);
    }

    public async Task<UserPersonLinkDto> LinkAsync(
        Guid personId,
        LinkUserToPersonDto dto,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.TenantId is null)
            throw new InvalidOperationException("Tenant context is required.");

        var person = await _db.People.FirstOrDefaultAsync(p => p.Id == personId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        var user = await ResolveUserAsync(dto, cancellationToken);

        var existingForUser = await _db.UserPersonLinks
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsActive, cancellationToken);
        if (existingForUser is not null && existingForUser.PersonId != personId)
            throw new InvalidOperationException("This user is already linked to another person in this tenant.");

        var existingForPerson = await _db.UserPersonLinks
            .FirstOrDefaultAsync(x => x.PersonId == personId && x.IsActive, cancellationToken);
        if (existingForPerson is not null && existingForPerson.UserId != user.Id)
            throw new InvalidOperationException("This person is already linked to another user.");

        if (existingForPerson is not null && existingForPerson.UserId == user.Id)
            return await MapAsync(existingForPerson, cancellationToken);

        // Reactivate soft-unlinked row for same pair if present
        var inactive = await _db.UserPersonLinks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == _tenant.TenantId && x.UserId == user.Id && x.PersonId == personId,
                cancellationToken);
        if (inactive is not null)
        {
            inactive.IsActive = true;
            inactive.TenantId = _tenant.TenantId.Value;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await MapAsync(inactive, cancellationToken);
        }

        var link = new UserPersonLink
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PersonId = person.Id,
            IsActive = true,
        };
        _db.UserPersonLinks.Add(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapAsync(link, cancellationToken);
    }

    public async Task UnlinkAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonLinks
            .FirstOrDefaultAsync(x => x.PersonId == personId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No active access link for this person.");

        link.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Guid? PersonId, IReadOnlyList<Guid> UnitIds)> GetPortalScopeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        if (link is null) return (null, Array.Empty<Guid>());

        var unitIds = await GetActiveUnitIdsAsync(link.PersonId, cancellationToken);
        return (link.PersonId, unitIds);
    }

    public async Task<PortalMeDto> GetPortalMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Person link required for portal access.");

        var person = await _db.People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == link.PersonId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Person link required for portal access.");

        var unitIds = await GetActiveUnitIdsAsync(link.PersonId, cancellationToken);
        return new PortalMeDto
        {
            PersonId = person.Id,
            PersonFullName = $"{person.FirstName} {person.LastName}".Trim(),
            UnitIds = unitIds,
        };
    }

    private async Task<User> ResolveUserAsync(LinkUserToPersonDto dto, CancellationToken cancellationToken)
    {
        if (dto.UserId is Guid userId && userId != Guid.Empty)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new InvalidOperationException("UserId or Email is required.");

        var normalized = dto.Email.Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken)
            ?? throw new InvalidOperationException("User not found for that email. Invite the user to the team first.");

        // Ensure membership in current tenant
        var membership = await _db.TenantUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                tu => tu.TenantId == _tenant.TenantId && tu.UserId == user.Id,
                cancellationToken);
        if (membership is null || !membership.IsActive)
            throw new InvalidOperationException("User is not an active member of this tenant. Invite them to the team first.");

        return user;
    }

    private async Task<IReadOnlyList<Guid>> GetActiveUnitIdsAsync(Guid personId, CancellationToken cancellationToken)
    {
        return await _db.UnitOccupancies.AsNoTracking()
            .Where(o => o.PersonId == personId && o.IsActive)
            .Select(o => o.UnitId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<UserPersonLinkDto> MapAsync(UserPersonLink link, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == link.UserId, cancellationToken);
        var person = await _db.People.AsNoTracking().FirstAsync(p => p.Id == link.PersonId, cancellationToken);
        var unitIds = await GetActiveUnitIdsAsync(link.PersonId, cancellationToken);
        return new UserPersonLinkDto
        {
            Id = link.Id,
            UserId = link.UserId,
            UserEmail = user.Email,
            PersonId = link.PersonId,
            PersonFullName = $"{person.FirstName} {person.LastName}".Trim(),
            IsActive = link.IsActive,
            UnitIds = unitIds,
        };
    }
}
