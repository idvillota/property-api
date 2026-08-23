using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823170000_AddDunningRules")]
    public partial class AddDunningRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DunningRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DaysOverdue = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CooldownDays = table.Column<int>(type: "integer", nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmailBodyTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DunningRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DunningRules_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DunningSendLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DunningRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DaysOverdueSnapshot = table.Column<int>(type: "integer", nullable: false),
                    BalanceSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DunningSendLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DunningSendLogs_DunningRules_DunningRuleId",
                        column: x => x.DunningRuleId,
                        principalTable: "DunningRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DunningSendLogs_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DunningSendLogs_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DunningSendLogs_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DunningRules_HorizontalPropertyId",
                table: "DunningRules",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningRules_TenantId_HorizontalPropertyId_DaysOverdue",
                table: "DunningRules",
                columns: new[] { "TenantId", "HorizontalPropertyId", "DaysOverdue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DunningRules_TenantId_IsEnabled",
                table: "DunningRules",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_DunningRuleId",
                table: "DunningSendLogs",
                column: "DunningRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_HorizontalPropertyId",
                table: "DunningSendLogs",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_PersonId",
                table: "DunningSendLogs",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_TenantId_DunningRuleId_UnitId_SentAtUtc",
                table: "DunningSendLogs",
                columns: new[] { "TenantId", "DunningRuleId", "UnitId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_TenantId_HorizontalPropertyId_SentAtUtc",
                table: "DunningSendLogs",
                columns: new[] { "TenantId", "HorizontalPropertyId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DunningSendLogs_UnitId",
                table: "DunningSendLogs",
                column: "UnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DunningSendLogs");
            migrationBuilder.DropTable(name: "DunningRules");
        }
    }
}
