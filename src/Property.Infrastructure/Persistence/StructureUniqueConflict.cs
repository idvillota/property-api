using Npgsql;

namespace Property.Infrastructure.Persistence;

/// <summary>
/// Active rows keep a unique name or code. Inactive rows stay for history and no longer block a replacement.
/// </summary>
public static class StructureUniqueConflict
{
    public const string ActiveOnly = "\"IsActive\" = TRUE";

    public const string BuildingNameIndex = "IX_Buildings_TenantId_HorizontalPropertyId_Name";
    public const string FloorNameIndex = "IX_Floors_TenantId_BuildingId_Name";
    public const string UnitCodeIndex = "IX_Units_TenantId_HorizontalPropertyId_Code";

    public static string? MessageFor(string? constraintName) => constraintName switch
    {
        BuildingNameIndex => "Ya existe una torre activa con ese nombre.",
        FloorNameIndex => "Ya existe un piso activo con ese nombre en esta torre.",
        UnitCodeIndex => "Ya existe una unidad activa con ese código.",
        _ => null,
    };

    public static bool TryTranslate(Exception exception, out InvalidOperationException conflict)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && MessageFor(postgres.ConstraintName) is { } message)
            {
                conflict = new InvalidOperationException(message);
                return true;
            }
        }

        conflict = null!;
        return false;
    }
}
