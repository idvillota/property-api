using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823140000_AddVisitLogs")]
    public partial class AddVisitLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitLogs_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_People_VisitorPersonId",
                        column: x => x.VisitorPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitLogs_Users_RegisteredByUserId",
                        column: x => x.RegisteredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_TenantId_HorizontalPropertyId_EnteredAtUtc",
                table: "VisitLogs",
                columns: new[] { "TenantId", "HorizontalPropertyId", "EnteredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_TenantId_UnitId_EnteredAtUtc",
                table: "VisitLogs",
                columns: new[] { "TenantId", "UnitId", "EnteredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_VisitorPersonId",
                table: "VisitLogs",
                column: "VisitorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_HorizontalPropertyId",
                table: "VisitLogs",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_UnitId",
                table: "VisitLogs",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_RegisteredByUserId",
                table: "VisitLogs",
                column: "RegisteredByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VisitLogs");
        }
    }
}
