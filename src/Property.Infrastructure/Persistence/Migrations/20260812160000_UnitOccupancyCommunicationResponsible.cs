using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260812160000_UnitOccupancyCommunicationResponsible")]
    public partial class UnitOccupancyCommunicationResponsible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommunicationResponsible",
                table: "UnitOccupancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Seed: prefer billing contact, else primary, else earliest active occupancy per unit.
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "UnitId"
                            ORDER BY
                                "ReceivesBilling" DESC,
                                "IsPrimary" DESC,
                                "StartDate" ASC,
                                "CreatedAtUtc" ASC
                        ) AS rn
                    FROM "UnitOccupancies"
                    WHERE "IsActive" = TRUE
                )
                UPDATE "UnitOccupancies" u
                SET "IsCommunicationResponsible" = TRUE
                FROM ranked r
                WHERE u."Id" = r."Id" AND r.rn = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommunicationResponsible",
                table: "UnitOccupancies");
        }
    }
}
