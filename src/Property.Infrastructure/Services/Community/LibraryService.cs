using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services.Community;

public sealed class LibraryService : ILibraryService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public LibraryService(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<LibraryDocumentDto> CreateAsync(
        CreateLibraryDocumentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.FileAssetId == Guid.Empty)
            throw new InvalidOperationException("El archivo es obligatorio.");
        var exists = await _db.FileAssets.AnyAsync(f => f.Id == dto.FileAssetId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Archivo no encontrado.");

        var doc = new LibraryDocument
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Title = dto.Title.Trim(),
            Category = dto.Category.Trim(),
            FileAssetId = dto.FileAssetId,
            Visibility = dto.Visibility,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _db.LibraryDocuments.AddAsync(doc, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(doc);
    }

    public async Task<IReadOnlyList<LibraryDocumentDto>> ListAdminAsync(
        Guid horizontalPropertyId, CancellationToken cancellationToken = default)
    {
        var list = await _db.LibraryDocuments.AsNoTracking()
            .Where(d => d.HorizontalPropertyId == horizontalPropertyId && d.IsActive)
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<LibraryDocumentDto>> ListPortalAsync(
        Guid horizontalPropertyId, bool isOwner, CancellationToken cancellationToken = default)
    {
        var list = await _db.LibraryDocuments.AsNoTracking()
            .Where(d => d.HorizontalPropertyId == horizontalPropertyId && d.IsActive)
            .ToListAsync(cancellationToken);

        return list.Where(d => d.Visibility switch
        {
            LibraryVisibility.AdminOnly => false,
            LibraryVisibility.Owners => isOwner,
            LibraryVisibility.Residents => true,
            LibraryVisibility.AllPortal => true,
            _ => false,
        }).Select(Map).ToList();
    }

    private Guid RequireTenant() =>
        _tenant.TenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant context is required.");

    private static LibraryDocumentDto Map(LibraryDocument d) => new()
    {
        Id = d.Id,
        HorizontalPropertyId = d.HorizontalPropertyId,
        Title = d.Title,
        Category = d.Category,
        FileAssetId = d.FileAssetId,
        Visibility = d.Visibility,
    };
}
