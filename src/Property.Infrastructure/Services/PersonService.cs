using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.People.Persons;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class PersonService : IPersonService
{
    private readonly IRepository<Person> _people;
    private readonly IUnitOfWork _unitOfWork;

    public PersonService(IRepository<Person> people, IUnitOfWork unitOfWork)
    {
        _people = people;
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
            IsActive = entity.IsActive,
        };
}
