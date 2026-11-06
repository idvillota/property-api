using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261103120000_ActiveStructureUniqueIndexes")]
    public partial class ActiveStructureUniqueIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Replace(migrationBuilder, "Buildings", "IX_Buildings_TenantId_HorizontalPropertyId_Name", new[] { "TenantId", "HorizontalPropertyId", "Name" });
            Replace(migrationBuilder, "Floors", "IX_Floors_TenantId_BuildingId_Name", new[] { "TenantId", "BuildingId", "Name" });
            Replace(migrationBuilder, "Units", "IX_Units_TenantId_HorizontalPropertyId_Code", new[] { "TenantId", "HorizontalPropertyId", "Code" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Replace(migrationBuilder, "Buildings", "IX_Buildings_TenantId_HorizontalPropertyId_Name", new[] { "TenantId", "HorizontalPropertyId", "Name" }, filtered: false);
            Replace(migrationBuilder, "Floors", "IX_Floors_TenantId_BuildingId_Name", new[] { "TenantId", "BuildingId", "Name" }, filtered: false);
            Replace(migrationBuilder, "Units", "IX_Units_TenantId_HorizontalPropertyId_Code", new[] { "TenantId", "HorizontalPropertyId", "Code" }, filtered: false);
        }

        private static void Replace(
            MigrationBuilder migrationBuilder,
            string table,
            string index,
            string[] columns,
            bool filtered = true)
        {
            migrationBuilder.DropIndex(name: index, table: table);
            migrationBuilder.CreateIndex(
                name: index,
                table: table,
                columns: columns,
                unique: true,
                filter: filtered ? StructureUniqueConflict.ActiveOnly : null);
        }
    }
}
