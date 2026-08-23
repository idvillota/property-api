using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823190000_AddMuralPostReadsAndPqrs")]
    public partial class AddMuralPostReadsAndPqrs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuralPostReads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MuralPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuralPostReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuralPostReads_MuralPosts_MuralPostId",
                        column: x => x.MuralPostId,
                        principalTable: "MuralPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MuralPostReads_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuralPostReads_TenantId_MuralPostId_PersonId",
                table: "MuralPostReads",
                columns: new[] { "TenantId", "MuralPostId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuralPostReads_MuralPostId",
                table: "MuralPostReads",
                column: "MuralPostId");

            migrationBuilder.CreateTable(
                name: "PqrsCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentFileAssetIdsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PqrsCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PqrsCases_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PqrsCases_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PqrsCases_People_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PqrsCases_TenantId_HorizontalPropertyId_Status",
                table: "PqrsCases",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PqrsCases_TenantId_CreatedByPersonId",
                table: "PqrsCases",
                columns: new[] { "TenantId", "CreatedByPersonId" });

            migrationBuilder.CreateTable(
                name: "PqrsComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PqrsCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PqrsComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PqrsComments_PqrsCases_PqrsCaseId",
                        column: x => x.PqrsCaseId,
                        principalTable: "PqrsCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PqrsComments_PqrsCaseId_PostedAtUtc",
                table: "PqrsComments",
                columns: new[] { "PqrsCaseId", "PostedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuralPostReads");
            migrationBuilder.DropTable(name: "PqrsComments");
            migrationBuilder.DropTable(name: "PqrsCases");
        }
    }
}
