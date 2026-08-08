using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Models;
using Property.Domain.Entities;

namespace Property.Infrastructure.Common;

internal static class PagedEntityQueries
{
    public static IQueryable<Employee> ShapeEmployees(IQueryable<Employee> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.FullName, EfTextSearch.LikePattern(search)) ||
                (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, EfTextSearch.LikePattern(search))));
        }

        if (q.FilterValue("fullName") is { } nameFilter)
            query = query.Where(e => EF.Functions.ILike(e.FullName, EfTextSearch.LikePattern(nameFilter)));

        if (q.FilterValue("jobTitle") is { } titleFilter)
            query = query.Where(e => e.JobTitle != null && EF.Functions.ILike(e.JobTitle, EfTextSearch.LikePattern(titleFilter)));

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        return ApplyEmployeeSort(query, q);
    }

    private static IQueryable<Employee> ApplyEmployeeSort(IQueryable<Employee> query, ListQuery q)
    {
        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "name") switch
        {
            "fullname" => desc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
            "jobtitle" => desc ? query.OrderByDescending(e => e.JobTitle) : query.OrderBy(e => e.JobTitle),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
        };
    }
}
