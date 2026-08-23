using System.Text.Json;
using Property.Domain.Enums;

namespace Property.Application.Features.Community.Mural;

/// <summary>Serialize/match mural audience JSON (empty lists = everyone).</summary>
public static class MuralAudience
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string SerializeGuids(IEnumerable<Guid>? ids)
    {
        var list = (ids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    public static string SerializeRoles(IEnumerable<OccupancyRole>? roles)
    {
        var list = (roles ?? Enumerable.Empty<OccupancyRole>()).Distinct().ToList();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    public static IReadOnlyList<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions)?.Distinct().ToList()
                   ?? (IReadOnlyList<Guid>)Array.Empty<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }

    public static IReadOnlyList<OccupancyRole> ParseRoles(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<OccupancyRole>();

        try
        {
            return JsonSerializer.Deserialize<List<OccupancyRole>>(json, JsonOptions)?.Distinct().ToList()
                   ?? (IReadOnlyList<OccupancyRole>)Array.Empty<OccupancyRole>();
        }
        catch (JsonException)
        {
            return Array.Empty<OccupancyRole>();
        }
    }

    public static bool IsBroadcast(
        IReadOnlyCollection<Guid> buildingIds,
        IReadOnlyCollection<Guid> floorIds,
        IReadOnlyCollection<OccupancyRole> roles) =>
        buildingIds.Count == 0 && floorIds.Count == 0 && roles.Count == 0;

    /// <summary>
    /// AND across dimensions; OR within a dimension. Empty audience dimension = match all.
    /// </summary>
    public static bool MatchesReader(
        IReadOnlyCollection<Guid> postBuildings,
        IReadOnlyCollection<Guid> postFloors,
        IReadOnlyCollection<OccupancyRole> postRoles,
        MuralReaderScope reader)
    {
        if (IsBroadcast(postBuildings, postFloors, postRoles))
            return true;

        if (postBuildings.Count > 0 && !postBuildings.Any(id => reader.BuildingIds.Contains(id)))
            return false;

        if (postFloors.Count > 0 && !postFloors.Any(id => reader.FloorIds.Contains(id)))
            return false;

        if (postRoles.Count > 0 && !postRoles.Any(r => reader.OccupancyRoles.Contains(r)))
            return false;

        return true;
    }

    public static string BuildSummary(
        IReadOnlyCollection<Guid> buildingIds,
        IReadOnlyCollection<Guid> floorIds,
        IReadOnlyCollection<OccupancyRole> roles,
        int matchingUnitCount,
        int matchingOccupancyCount)
    {
        if (IsBroadcast(buildingIds, floorIds, roles))
            return "Todos los residentes y propietarios del conjunto";

        var parts = new List<string>();
        if (buildingIds.Count > 0)
            parts.Add($"{buildingIds.Count} torre{(buildingIds.Count == 1 ? "" : "s")}");
        if (floorIds.Count > 0)
            parts.Add($"{floorIds.Count} piso{(floorIds.Count == 1 ? "" : "s")}");
        if (roles.Count > 0)
            parts.Add($"{roles.Count} rol{(roles.Count == 1 ? "" : "es")}");

        return $"Segmento: {string.Join(" · ", parts)} → ~{matchingUnitCount} unidad{(matchingUnitCount == 1 ? "" : "es")}, {matchingOccupancyCount} ocupación{(matchingOccupancyCount == 1 ? "" : "es")}";
    }
}
