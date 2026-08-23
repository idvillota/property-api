using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Models;
using Property.Domain.Entities;
using Property.Domain.Enums;

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

    public static IQueryable<Neighborhood> ShapeNeighborhoods(IQueryable<Neighborhood> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)) ||
                (e.City != null && EF.Functions.ILike(e.City, EfTextSearch.LikePattern(search))));
        }

        if (q.FilterValue("city") is { } cityFilter)
            query = query.Where(e => e.City != null && EF.Functions.ILike(e.City, EfTextSearch.LikePattern(cityFilter)));

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "name") switch
        {
            "city" => desc ? query.OrderByDescending(e => e.City) : query.OrderBy(e => e.City),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
        };
    }

    public static IQueryable<VehicleBrand> ShapeVehicleBrands(IQueryable<VehicleBrand> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
            query = query.Where(e => EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)));

        if (q.FilterValue("name") is { } nameFilter)
            query = query.Where(e => EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(nameFilter)));

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "name") switch
        {
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
        };
    }

    public static IQueryable<HorizontalProperty> ShapeHorizontalProperties(IQueryable<HorizontalProperty> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)) ||
                (e.TradeName != null && EF.Functions.ILike(e.TradeName, EfTextSearch.LikePattern(search))) ||
                EF.Functions.ILike(e.City, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.AddressLine, EfTextSearch.LikePattern(search)));
        }

        if (q.FilterValue("neighborhoodId") is { } neighborhoodRaw &&
            ListQueryHelpers.TryParseGuid(neighborhoodRaw, out var neighborhoodId))
            query = query.Where(e => e.NeighborhoodId == neighborhoodId);

        if (q.FilterValue("city") is { } cityFilter)
            query = query.Where(e => EF.Functions.ILike(e.City, EfTextSearch.LikePattern(cityFilter)));

        if (q.FilterValue("propertyUseType") is { } useRaw &&
            Enum.TryParse<PropertyUseType>(useRaw, ignoreCase: true, out var useType))
            query = query.Where(e => e.PropertyUseType == useType);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "name") switch
        {
            "city" => desc ? query.OrderByDescending(e => e.City) : query.OrderBy(e => e.City),
            "propertyusetype" => desc ? query.OrderByDescending(e => e.PropertyUseType) : query.OrderBy(e => e.PropertyUseType),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
        };
    }

    public static IQueryable<Building> ShapeBuildings(IQueryable<Building> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)) ||
                (e.Code != null && EF.Functions.ILike(e.Code, EfTextSearch.LikePattern(search))));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "sortorder") switch
        {
            "name" => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "code" => desc ? query.OrderByDescending(e => e.Code) : query.OrderBy(e => e.Code),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc
                ? query.OrderByDescending(e => e.SortOrder).ThenByDescending(e => e.Name)
                : query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name),
        };
    }

    public static IQueryable<Floor> ShapeFloors(IQueryable<Floor> query, IQueryable<Building> buildings, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
            query = query.Where(e => EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)));

        if (q.FilterValue("buildingId") is { } buildingRaw &&
            ListQueryHelpers.TryParseGuid(buildingRaw, out var buildingId))
            query = query.Where(e => e.BuildingId == buildingId);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
        {
            query = query.Where(e =>
                buildings.Any(b => b.Id == e.BuildingId && b.HorizontalPropertyId == horizontalPropertyId));
        }

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "sortorder") switch
        {
            "name" => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "number" => desc ? query.OrderByDescending(e => e.Number) : query.OrderBy(e => e.Number),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc
                ? query.OrderByDescending(e => e.SortOrder).ThenByDescending(e => e.Name)
                : query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name),
        };
    }

    public static IQueryable<Unit> ShapeUnits(IQueryable<Unit> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Code, EfTextSearch.LikePattern(search)) ||
                (e.ContributionModuleCode != null &&
                 EF.Functions.ILike(e.ContributionModuleCode, EfTextSearch.LikePattern(search))));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("buildingId") is { } buildingRaw &&
            ListQueryHelpers.TryParseGuid(buildingRaw, out var buildingId))
            query = query.Where(e => e.BuildingId == buildingId);

        if (q.FilterValue("floorId") is { } floorRaw &&
            ListQueryHelpers.TryParseGuid(floorRaw, out var floorId))
            query = query.Where(e => e.FloorId == floorId);

        if (q.FilterValue("unitType") is { } unitTypeRaw &&
            Enum.TryParse<UnitType>(unitTypeRaw, ignoreCase: true, out var unitType))
            query = query.Where(e => e.UnitType == unitType);

        if (q.FilterValue("isBillable") is { } billableRaw &&
            ListQueryHelpers.TryParseBool(billableRaw, out var isBillable))
            query = query.Where(e => e.IsBillable == isBillable);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "code") switch
        {
            "coefficient" => desc ? query.OrderByDescending(e => e.Coefficient) : query.OrderBy(e => e.Coefficient),
            "privatebuiltaream2" => desc
                ? query.OrderByDescending(e => e.PrivateBuiltAreaM2)
                : query.OrderBy(e => e.PrivateBuiltAreaM2),
            "unittype" => desc ? query.OrderByDescending(e => e.UnitType) : query.OrderBy(e => e.UnitType),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Code) : query.OrderBy(e => e.Code),
        };
    }

    public static IQueryable<Vehicle> ShapeVehicles(IQueryable<Vehicle> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Plate, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.Brand, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.Model, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.Color, EfTextSearch.LikePattern(search)));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("unitId") is { } unitRaw)
        {
            if (string.Equals(unitRaw, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(unitRaw, "unassigned", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.UnitId == null);
            else if (ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
                query = query.Where(e => e.UnitId == unitId);
        }

        if (q.FilterValue("excludeUnitId") is { } excludeUnitRaw &&
            ListQueryHelpers.TryParseGuid(excludeUnitRaw, out var excludeUnitId))
            query = query.Where(e => e.UnitId != excludeUnitId);

        if (q.FilterValue("vehicleType") is { } typeRaw &&
            Enum.TryParse<VehicleType>(typeRaw, ignoreCase: true, out var vehicleType))
            query = query.Where(e => e.VehicleType == vehicleType);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "plate") switch
        {
            "brand" => desc ? query.OrderByDescending(e => e.Brand) : query.OrderBy(e => e.Brand),
            "model" => desc ? query.OrderByDescending(e => e.Model) : query.OrderBy(e => e.Model),
            "vehicletype" => desc
                ? query.OrderByDescending(e => e.VehicleType)
                : query.OrderBy(e => e.VehicleType),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Plate) : query.OrderBy(e => e.Plate),
        };
    }

    public static IQueryable<Person> ShapePeople(IQueryable<Person> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            // Match each token against name/document/email so "Ana Pérez" works.
            foreach (var term in search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pattern = EfTextSearch.LikePattern(term);
                query = query.Where(e =>
                    EF.Functions.ILike(e.FirstName, pattern) ||
                    EF.Functions.ILike(e.LastName, pattern) ||
                    EF.Functions.ILike(e.FirstName + " " + e.LastName, pattern) ||
                    EF.Functions.ILike(e.DocumentNumber, pattern) ||
                    (e.Email != null && EF.Functions.ILike(e.Email, pattern)));
            }
        }

        if (q.FilterValue("documentType") is { } docTypeRaw &&
            Enum.TryParse<DocumentType>(docTypeRaw, ignoreCase: true, out var documentType))
            query = query.Where(e => e.DocumentType == documentType);

        if (q.FilterValue("documentNumber") is { } documentNumber)
            query = query.Where(e => EF.Functions.ILike(e.DocumentNumber, EfTextSearch.LikePattern(documentNumber)));

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "lastname") switch
        {
            "firstname" => desc ? query.OrderByDescending(e => e.FirstName) : query.OrderBy(e => e.FirstName),
            "documentnumber" => desc ? query.OrderByDescending(e => e.DocumentNumber) : query.OrderBy(e => e.DocumentNumber),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc
                ? query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName)
                : query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName),
        };
    }

    public static IQueryable<UnitOccupancy> ShapeUnitOccupancies(
        IQueryable<UnitOccupancy> query,
        IQueryable<Unit> units,
        ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("personId") is { } personRaw &&
            ListQueryHelpers.TryParseGuid(personRaw, out var personId))
            query = query.Where(e => e.PersonId == personId);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
        {
            query = query.Where(e =>
                units.Any(u => u.Id == e.UnitId && u.HorizontalPropertyId == horizontalPropertyId));
        }

        if (q.FilterValue("role") is { } roleRaw &&
            Enum.TryParse<OccupancyRole>(roleRaw, ignoreCase: true, out var role))
            query = query.Where(e => e.Role == role);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "startdate") switch
        {
            "role" => desc ? query.OrderByDescending(e => e.Role) : query.OrderBy(e => e.Role),
            "enddate" => desc ? query.OrderByDescending(e => e.EndDate) : query.OrderBy(e => e.EndDate),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.StartDate) : query.OrderBy(e => e.StartDate),
        };
    }

    public static IQueryable<BillingPeriod> ShapeBillingPeriods(IQueryable<BillingPeriod> query, ListQuery q)
    {
        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<BillingPeriodStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.FilterValue("year") is { } yearRaw &&
            ListQueryHelpers.TryParseInt(yearRaw, out var year))
            query = query.Where(e => e.Year == year);

        if (q.FilterValue("month") is { } monthRaw &&
            ListQueryHelpers.TryParseInt(monthRaw, out var month))
            query = query.Where(e => e.Month == month);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "period") switch
        {
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            "year" => desc ? query.OrderByDescending(e => e.Year) : query.OrderBy(e => e.Year),
            "month" => desc ? query.OrderByDescending(e => e.Month) : query.OrderBy(e => e.Month),
            _ => desc
                ? query.OrderByDescending(e => e.Year).ThenByDescending(e => e.Month)
                : query.OrderBy(e => e.Year).ThenBy(e => e.Month),
        };
    }

    public static IQueryable<FeeCharge> ShapeFeeCharges(IQueryable<FeeCharge> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("billingPeriodId") is { } periodRaw &&
            ListQueryHelpers.TryParseGuid(periodRaw, out var billingPeriodId))
            query = query.Where(e => e.BillingPeriodId == billingPeriodId);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<FeeChargeStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.FilterValue("chargeType") is { } typeRaw &&
            Enum.TryParse<FeeChargeType>(typeRaw, ignoreCase: true, out var chargeType))
            query = query.Where(e => e.ChargeType == chargeType);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
            query = query.Where(e => EF.Functions.ILike(e.Description, EfTextSearch.LikePattern(search)));

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "duedate") switch
        {
            "amount" => desc ? query.OrderByDescending(e => e.Amount) : query.OrderBy(e => e.Amount),
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            "chargetype" => desc ? query.OrderByDescending(e => e.ChargeType) : query.OrderBy(e => e.ChargeType),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.DueDate) : query.OrderBy(e => e.DueDate),
        };
    }

    public static IQueryable<Payment> ShapePayments(IQueryable<Payment> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("personId") is { } personRaw &&
            ListQueryHelpers.TryParseGuid(personRaw, out var personId))
            query = query.Where(e => e.PersonId == personId);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                (e.Reference != null && EF.Functions.ILike(e.Reference, EfTextSearch.LikePattern(search))) ||
                (e.Notes != null && EF.Functions.ILike(e.Notes, EfTextSearch.LikePattern(search))));
        }

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "paidat") switch
        {
            "amount" => desc ? query.OrderByDescending(e => e.Amount) : query.OrderBy(e => e.Amount),
            "method" => desc ? query.OrderByDescending(e => e.Method) : query.OrderBy(e => e.Method),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.PaidAt) : query.OrderBy(e => e.PaidAt),
        };
    }

    public static IQueryable<CommonArea> ShapeCommonAreas(IQueryable<CommonArea> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Name, EfTextSearch.LikePattern(search)) ||
                (e.RulesNotes != null && EF.Functions.ILike(e.RulesNotes, EfTextSearch.LikePattern(search))));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("areaType") is { } areaTypeRaw &&
            Enum.TryParse<CommonAreaType>(areaTypeRaw, ignoreCase: true, out var areaType))
            query = query.Where(e => e.AreaType == areaType);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "name") switch
        {
            "areatype" => desc ? query.OrderByDescending(e => e.AreaType) : query.OrderBy(e => e.AreaType),
            "capacity" => desc ? query.OrderByDescending(e => e.Capacity) : query.OrderBy(e => e.Capacity),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
        };
    }

    public static IQueryable<Reservation> ShapeReservations(IQueryable<Reservation> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("commonAreaId") is { } areaRaw &&
            ListQueryHelpers.TryParseGuid(areaRaw, out var commonAreaId))
            query = query.Where(e => e.CommonAreaId == commonAreaId);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("personId") is { } personRaw &&
            ListQueryHelpers.TryParseGuid(personRaw, out var personId))
            query = query.Where(e => e.PersonId == personId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<ReservationStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.FilterValue("fromUtc") is { } fromRaw &&
            ListQueryHelpers.TryParseDateTimeUtc(fromRaw, out var fromUtc))
            query = query.Where(e => e.EndsAtUtc > fromUtc);

        if (q.FilterValue("toUtc") is { } toRaw &&
            ListQueryHelpers.TryParseDateTimeUtc(toRaw, out var toUtc))
            query = query.Where(e => e.StartsAtUtc < toUtc);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
            query = query.Where(e => e.Notes != null && EF.Functions.ILike(e.Notes, EfTextSearch.LikePattern(search)));

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "startsatutc") switch
        {
            "endsatutc" => desc ? query.OrderByDescending(e => e.EndsAtUtc) : query.OrderBy(e => e.EndsAtUtc),
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            "attendeescount" => desc ? query.OrderByDescending(e => e.AttendeesCount) : query.OrderBy(e => e.AttendeesCount),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.StartsAtUtc) : query.OrderBy(e => e.StartsAtUtc),
        };
    }

    public static IQueryable<ReceptionItem> ShapeReceptionItems(IQueryable<ReceptionItem> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<ReceptionItemStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.FilterValue("itemType") is { } typeRaw &&
            Enum.TryParse<ReceptionItemType>(typeRaw, ignoreCase: true, out var itemType))
            query = query.Where(e => e.ItemType == itemType);

        if (q.FilterValue("direction") is { } directionRaw &&
            Enum.TryParse<ReceptionDirection>(directionRaw, ignoreCase: true, out var direction))
            query = query.Where(e => e.Direction == direction);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                (e.CarrierOrSender != null && EF.Functions.ILike(e.CarrierOrSender, EfTextSearch.LikePattern(search))) ||
                (e.TrackingOrReference != null && EF.Functions.ILike(e.TrackingOrReference, EfTextSearch.LikePattern(search))) ||
                (e.RecipientName != null && EF.Functions.ILike(e.RecipientName, EfTextSearch.LikePattern(search))) ||
                (e.AuthorizedPickupName1 != null && EF.Functions.ILike(e.AuthorizedPickupName1, EfTextSearch.LikePattern(search))) ||
                (e.AuthorizedPickupName2 != null && EF.Functions.ILike(e.AuthorizedPickupName2, EfTextSearch.LikePattern(search))) ||
                (e.DeliveredToExternalName != null && EF.Functions.ILike(e.DeliveredToExternalName, EfTextSearch.LikePattern(search))) ||
                (e.Notes != null && EF.Functions.ILike(e.Notes, EfTextSearch.LikePattern(search))));
        }

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "receivedatutc") switch
        {
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            "itemtype" => desc ? query.OrderByDescending(e => e.ItemType) : query.OrderBy(e => e.ItemType),
            "direction" => desc ? query.OrderByDescending(e => e.Direction) : query.OrderBy(e => e.Direction),
            "deliveredatutc" => desc ? query.OrderByDescending(e => e.DeliveredAtUtc) : query.OrderBy(e => e.DeliveredAtUtc),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.ReceivedAtUtc) : query.OrderBy(e => e.ReceivedAtUtc),
        };
    }

    public static IQueryable<MuralPost> ShapeMuralPosts(IQueryable<MuralPost> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var includeHidden = q.FilterValue("includeHidden") is { } hiddenRaw &&
                            ListQueryHelpers.TryParseBool(hiddenRaw, out var showHidden) &&
                            showHidden;
        if (!includeHidden)
            query = query.Where(e => !e.IsHidden);

        var includeExpired = q.FilterValue("includeExpired") is { } expiredRaw &&
                             ListQueryHelpers.TryParseBool(expiredRaw, out var showExpired) &&
                             showExpired;
        if (!includeExpired)
        {
            var now = DateTime.UtcNow;
            query = query.Where(e => e.ExpiresAtUtc == null || e.ExpiresAtUtc >= now);
        }

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Title, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.Body, EfTextSearch.LikePattern(search)) ||
                (e.AreaLabel != null && EF.Functions.ILike(e.AreaLabel, EfTextSearch.LikePattern(search))) ||
                EF.Functions.ILike(e.AuthorDisplayName, EfTextSearch.LikePattern(search)));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("kind") is { } kindRaw &&
            Enum.TryParse<MuralPostKind>(kindRaw, ignoreCase: true, out var kind))
            query = query.Where(e => e.Kind == kind);

        if (q.FilterValue("category") is { } categoryRaw &&
            Enum.TryParse<MuralPostCategory>(categoryRaw, ignoreCase: true, out var category))
            query = query.Where(e => e.Category == category);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        // Feed order: pinned first, then newest.
        return query
            .OrderByDescending(e => e.IsPinned)
            .ThenByDescending(e => e.PublishedAtUtc);
    }

    public static IQueryable<PqrsCase> ShapePqrsCases(IQueryable<PqrsCase> query, ListQuery q)
    {
        if (!q.IncludeInactive)
            query = query.Where(e => e.IsActive);

        var search = ListQueryHelpers.SearchTerm(q);
        if (search.Length > 0)
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Subject, EfTextSearch.LikePattern(search)) ||
                EF.Functions.ILike(e.Description, EfTextSearch.LikePattern(search)));
        }

        if (q.FilterValue("horizontalPropertyId") is { } hpRaw &&
            ListQueryHelpers.TryParseGuid(hpRaw, out var horizontalPropertyId))
            query = query.Where(e => e.HorizontalPropertyId == horizontalPropertyId);

        if (q.FilterValue("status") is { } statusRaw &&
            Enum.TryParse<PqrsStatus>(statusRaw, ignoreCase: true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.FilterValue("category") is { } categoryRaw &&
            Enum.TryParse<PqrsCategory>(categoryRaw, ignoreCase: true, out var category))
            query = query.Where(e => e.Category == category);

        if (q.FilterValue("unitId") is { } unitRaw &&
            ListQueryHelpers.TryParseGuid(unitRaw, out var unitId))
            query = query.Where(e => e.UnitId == unitId);

        if (q.FilterValue("isActive") is { } activeRaw &&
            ListQueryHelpers.TryParseBool(activeRaw, out var isActive))
            query = query.Where(e => e.IsActive == isActive);

        var desc = q.IsDescending;
        return (q.SortBy?.ToLowerInvariant() ?? "createdatutc") switch
        {
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            "category" => desc ? query.OrderByDescending(e => e.Category) : query.OrderBy(e => e.Category),
            "subject" => desc ? query.OrderByDescending(e => e.Subject) : query.OrderBy(e => e.Subject),
            "dueatutc" => desc ? query.OrderByDescending(e => e.DueAtUtc) : query.OrderBy(e => e.DueAtUtc),
            "isactive" => desc ? query.OrderByDescending(e => e.IsActive) : query.OrderBy(e => e.IsActive),
            _ => desc ? query.OrderByDescending(e => e.CreatedAtUtc) : query.OrderBy(e => e.CreatedAtUtc),
        };
    }
}

