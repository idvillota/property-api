using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823240000_AddHoldingWhatsAppWebhooks")]
    public partial class AddHoldingWhatsAppWebhooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties");

            migrationBuilder.CreateIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties",
                column: "TenantId");

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "People",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId_HorizontalPropertyId_SentAtUtc",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "HorizontalPropertyId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId_PersonId_SentAtUtc",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "PersonId", "SentAtUtc" });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Secret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId_IsActive",
                table: "WebhookSubscriptions",
                columns: new[] { "TenantId", "IsActive" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WebhookSubscriptions");
            migrationBuilder.DropTable(name: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties");

            migrationBuilder.CreateIndex(
                name: "IX_HorizontalProperties_TenantId",
                table: "HorizontalProperties",
                column: "TenantId",
                unique: true);
        }
    }
}
