using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneTenantOneProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HorizontalProperties_TenantId_Name",
                table: "HorizontalProperties");

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "HorizontalProperties",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalRepresentative",
                table: "HorizontalProperties",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "HorizontalProperties",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "HorizontalProperties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegime",
                table: "HorizontalProperties",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "HorizontalProperties" hp
                SET
                    "LegalName" = COALESCE(NULLIF(BTRIM(ts."LegalName"), ''), hp."Name"),
                    "TaxRegime" = NULLIF(BTRIM(ts."TaxRegime"), ''),
                    "LegalRepresentative" = NULLIF(BTRIM(ts."LegalRepresentative"), ''),
                    "PostalCode" = NULLIF(BTRIM(ts."PostalCode"), ''),
                    "Phone" = NULLIF(BTRIM(ts."Phone"), ''),
                    "Nit" = COALESCE(NULLIF(BTRIM(hp."Nit"), ''), NULLIF(BTRIM(ts."TaxId"), '')),
                    "TradeName" = COALESCE(NULLIF(BTRIM(hp."TradeName"), ''), NULLIF(BTRIM(ts."TradeName"), ''))
                FROM "TenantSettings" ts
                WHERE hp."TenantId" = ts."TenantId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "HorizontalProperties");

            migrationBuilder.DropColumn(
                name: "LegalRepresentative",
                table: "HorizontalProperties");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "HorizontalProperties");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "HorizontalProperties");

            migrationBuilder.DropColumn(
                name: "TaxRegime",
                table: "HorizontalProperties");

            migrationBuilder.CreateIndex(
                name: "IX_HorizontalProperties_TenantId_Name",
                table: "HorizontalProperties",
                columns: new[] { "TenantId", "Name" });
        }
    }
}
