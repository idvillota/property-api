using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260812120000_ReceptionDirectionAndOutboundPickups")]
    public partial class ReceptionDirectionAndOutboundPickups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "ReceptionItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedPickupName1",
                table: "ReceptionItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedPickupName2",
                table: "ReceptionItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredToExternalName",
                table: "ReceptionItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_TenantId_HorizontalPropertyId_Direction_Status",
                table: "ReceptionItems",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Direction", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReceptionItems_TenantId_HorizontalPropertyId_Direction_Status",
                table: "ReceptionItems");

            migrationBuilder.DropColumn(name: "Direction", table: "ReceptionItems");
            migrationBuilder.DropColumn(name: "AuthorizedPickupName1", table: "ReceptionItems");
            migrationBuilder.DropColumn(name: "AuthorizedPickupName2", table: "ReceptionItems");
            migrationBuilder.DropColumn(name: "DeliveredToExternalName", table: "ReceptionItems");
        }
    }
}
