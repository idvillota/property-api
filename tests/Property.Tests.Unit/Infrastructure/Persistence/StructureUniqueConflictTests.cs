using Property.Infrastructure.Persistence;

namespace Property.Tests.Unit.Infrastructure.Persistence;

public sealed class StructureUniqueConflictTests
{
    [Theory]
    [InlineData(StructureUniqueConflict.BuildingNameIndex, "Ya existe una torre activa con ese nombre.")]
    [InlineData(StructureUniqueConflict.FloorNameIndex, "Ya existe un piso activo con ese nombre en esta torre.")]
    [InlineData(StructureUniqueConflict.UnitCodeIndex, "Ya existe una unidad activa con ese código.")]
    public void MessageFor_names_the_active_duplicate(string constraint, string message)
    {
        Assert.Equal(message, StructureUniqueConflict.MessageFor(constraint));
    }

    [Fact]
    public void MessageFor_ignores_other_constraints()
    {
        Assert.Null(StructureUniqueConflict.MessageFor("IX_People_TenantId_DocumentType_DocumentNumber"));
        Assert.Null(StructureUniqueConflict.MessageFor(null));
    }
}
