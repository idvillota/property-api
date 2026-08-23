using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823110000_AddPaymentIntents")]
    public partial class AddPaymentIntents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProviderRef = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AllocationsJson = table.Column<string>(type: "text", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentIntents_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntents_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntents_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentIntents_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_IdempotencyKey",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_Status_CreatedAtUtc",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_HorizontalPropertyId",
                table: "PaymentIntents",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_UnitId",
                table: "PaymentIntents",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_PersonId",
                table: "PaymentIntents",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_PaymentId",
                table: "PaymentIntents",
                column: "PaymentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PaymentIntents");
        }
    }
}
