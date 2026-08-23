using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823160000_AddPaymentIntentProvider")]
    public partial class AddPaymentIntentProvider : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "PaymentIntents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Wompi");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_ProviderRef",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "ProviderRef" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentIntents_TenantId_ProviderRef",
                table: "PaymentIntents");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "PaymentIntents");
        }
    }
}
