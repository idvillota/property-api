using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823210000_AddPaymentReceiptAndAuditFilters")]
    public partial class AddPaymentReceiptAndAuditFilters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptFileAssetId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptFileAssetId",
                table: "Payments",
                column: "ReceiptFileAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_FileAssets_ReceiptFileAssetId",
                table: "Payments",
                column: "ReceiptFileAssetId",
                principalTable: "FileAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddColumn<bool>(
                name: "EmailReceiptOnPaymentEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_FileAssets_ReceiptFileAssetId",
                table: "Payments");
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptFileAssetId",
                table: "Payments");
            migrationBuilder.DropColumn(name: "ReceiptFileAssetId", table: "Payments");
            migrationBuilder.DropColumn(name: "EmailReceiptOnPaymentEnabled", table: "TenantSettings");
        }
    }
}
