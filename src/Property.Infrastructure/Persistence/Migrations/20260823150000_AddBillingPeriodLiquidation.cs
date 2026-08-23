using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823150000_AddBillingPeriodLiquidation")]
    public partial class AddBillingPeriodLiquidation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLiquidated",
                table: "BillingPeriods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LiquidatedAtUtc",
                table: "BillingPeriods",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: periods that already have active ordinary charges are liquidated.
            migrationBuilder.Sql(
                """
                UPDATE "BillingPeriods" bp
                SET "IsLiquidated" = TRUE,
                    "LiquidatedAtUtc" = COALESCE(bp."LiquidatedAtUtc", NOW() AT TIME ZONE 'utc')
                WHERE EXISTS (
                    SELECT 1
                    FROM "FeeCharges" fc
                    WHERE fc."BillingPeriodId" = bp."Id"
                      AND fc."ChargeType" = 0
                      AND fc."IsActive" = TRUE
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsLiquidated", table: "BillingPeriods");
            migrationBuilder.DropColumn(name: "LiquidatedAtUtc", table: "BillingPeriods");
        }
    }
}
