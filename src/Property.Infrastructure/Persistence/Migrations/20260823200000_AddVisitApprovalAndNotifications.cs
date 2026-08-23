using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823200000_AddVisitApprovalAndNotifications")]
    public partial class AddVisitApprovalAndNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalExpiresAtUtc",
                table: "VisitLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAtUtc",
                table: "VisitLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByPersonId",
                table: "VisitLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByUserId",
                table: "VisitLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                table: "VisitLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitLogs_DecidedByPersonId",
                table: "VisitLogs",
                column: "DecidedByPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitLogs_People_DecidedByPersonId",
                table: "VisitLogs",
                column: "DecidedByPersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_TenantId_PersonId_CreatedAtUtc",
                table: "InAppNotifications",
                columns: new[] { "TenantId", "PersonId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InAppNotifications");
            migrationBuilder.DropForeignKey(
                name: "FK_VisitLogs_People_DecidedByPersonId",
                table: "VisitLogs");
            migrationBuilder.DropIndex(
                name: "IX_VisitLogs_DecidedByPersonId",
                table: "VisitLogs");
            migrationBuilder.DropColumn(name: "ApprovalExpiresAtUtc", table: "VisitLogs");
            migrationBuilder.DropColumn(name: "DecidedAtUtc", table: "VisitLogs");
            migrationBuilder.DropColumn(name: "DecidedByPersonId", table: "VisitLogs");
            migrationBuilder.DropColumn(name: "DecidedByUserId", table: "VisitLogs");
            migrationBuilder.DropColumn(name: "OverrideReason", table: "VisitLogs");
        }
    }
}
