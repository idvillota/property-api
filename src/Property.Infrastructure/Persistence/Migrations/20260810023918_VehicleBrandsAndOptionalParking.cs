using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VehicleBrandsAndOptionalParking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "Vehicles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "VehicleBrands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBrands", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleBrandId",
                table: "Vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "VehicleBrands" ("Id", "CreatedAtUtc", "UpdatedAtUtc", "TenantId", "Name", "IsActive")
                SELECT gen_random_uuid(), NOW() AT TIME ZONE 'utc', NULL, v."TenantId", v."Brand", TRUE
                FROM (
                    SELECT DISTINCT "TenantId", TRIM("Brand") AS "Brand"
                    FROM "Vehicles"
                    WHERE TRIM("Brand") <> ''
                ) v;

                UPDATE "Vehicles" veh
                SET "VehicleBrandId" = b."Id"
                FROM "VehicleBrands" b
                WHERE b."TenantId" = veh."TenantId"
                  AND b."Name" = TRIM(veh."Brand");

                INSERT INTO "VehicleBrands" ("Id", "CreatedAtUtc", "UpdatedAtUtc", "TenantId", "Name", "IsActive")
                SELECT gen_random_uuid(), NOW() AT TIME ZONE 'utc', NULL, orphan."TenantId", 'Sin marca', TRUE
                FROM (
                    SELECT DISTINCT "TenantId"
                    FROM "Vehicles"
                    WHERE "VehicleBrandId" IS NULL
                ) orphan
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "VehicleBrands" existing
                    WHERE existing."TenantId" = orphan."TenantId"
                      AND existing."Name" = 'Sin marca'
                );

                UPDATE "Vehicles" veh
                SET "VehicleBrandId" = b."Id",
                    "Brand" = b."Name"
                FROM "VehicleBrands" b
                WHERE veh."VehicleBrandId" IS NULL
                  AND b."TenantId" = veh."TenantId"
                  AND b."Name" = 'Sin marca';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "VehicleBrandId",
                table: "Vehicles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleBrandId",
                table: "Vehicles",
                column: "VehicleBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBrands_TenantId_Name",
                table: "VehicleBrands",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleBrands_VehicleBrandId",
                table: "Vehicles",
                column: "VehicleBrandId",
                principalTable: "VehicleBrands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleBrands_VehicleBrandId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleBrands");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VehicleBrandId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleBrandId",
                table: "Vehicles");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "Vehicles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
