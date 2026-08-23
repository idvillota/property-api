using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823090000_AddTenantModuleFlags")]
    public partial class AddTenantModuleFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModulePortalEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleDigitalPaymentsEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleAccountingEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleVisitsEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleAssembliesEnabled",
                table: "TenantSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ModulePortalEnabled", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "ModuleDigitalPaymentsEnabled", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "ModuleAccountingEnabled", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "ModuleVisitsEnabled", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "ModuleAssembliesEnabled", table: "TenantSettings");
        }
    }
}
