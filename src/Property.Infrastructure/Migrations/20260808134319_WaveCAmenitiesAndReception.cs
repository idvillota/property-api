using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WaveCAmenitiesAndReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommonAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaType = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    RequiresDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RulesNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MinDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommonAreas_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarrierOrSender = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TrackingOrReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceptionItems_Employees_DeliveredByEmployeeId",
                        column: x => x.DeliveredByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceptionItems_Employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceptionItems_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceptionItems_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceptionItems_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommonAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttendeesCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DepositPaid = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_CommonAreas_CommonAreaId",
                        column: x => x.CommonAreaId,
                        principalTable: "CommonAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommonAreas_HorizontalPropertyId",
                table: "CommonAreas",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommonAreas_TenantId_HorizontalPropertyId_AreaType",
                table: "CommonAreas",
                columns: new[] { "TenantId", "HorizontalPropertyId", "AreaType" });

            migrationBuilder.CreateIndex(
                name: "IX_CommonAreas_TenantId_HorizontalPropertyId_Name",
                table: "CommonAreas",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_DeliveredByEmployeeId",
                table: "ReceptionItems",
                column: "DeliveredByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_HorizontalPropertyId",
                table: "ReceptionItems",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_PersonId",
                table: "ReceptionItems",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_ReceivedByEmployeeId",
                table: "ReceptionItems",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_TenantId_HorizontalPropertyId_Status",
                table: "ReceptionItems",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_TenantId_UnitId_Status",
                table: "ReceptionItems",
                columns: new[] { "TenantId", "UnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionItems_UnitId",
                table: "ReceptionItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CommonAreaId_StartsAtUtc",
                table: "Reservations",
                columns: new[] { "CommonAreaId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_HorizontalPropertyId",
                table: "Reservations",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PersonId",
                table: "Reservations",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_TenantId_HorizontalPropertyId_Status",
                table: "Reservations",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UnitId",
                table: "Reservations",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceptionItems");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "CommonAreas");
        }
    }
}
