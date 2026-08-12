using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260812031500_ReceptionDeliveredToPerson")]
    public partial class ReceptionDeliveredToPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveredToPersonId",
                table: "ReceptionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_DeliveredToPersonId",
                table: "ReceptionItems",
                column: "DeliveredToPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceptionItems_People_DeliveredToPersonId",
                table: "ReceptionItems",
                column: "DeliveredToPersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceptionItems_People_DeliveredToPersonId",
                table: "ReceptionItems");

            migrationBuilder.DropIndex(
                name: "IX_ReceptionItems_DeliveredToPersonId",
                table: "ReceptionItems");

            migrationBuilder.DropColumn(
                name: "DeliveredToPersonId",
                table: "ReceptionItems");
        }
    }
}
