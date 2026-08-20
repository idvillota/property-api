using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260813010000_AddMuralPosts")]
    public partial class AddMuralPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuralPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizontalPropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    ContactNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AreaLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuralPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuralPosts_HorizontalProperties_HorizontalPropertyId",
                        column: x => x.HorizontalPropertyId,
                        principalTable: "HorizontalProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuralPosts_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuralPosts_HorizontalPropertyId",
                table: "MuralPosts",
                column: "HorizontalPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MuralPosts_UnitId",
                table: "MuralPosts",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_MuralPosts_TenantId_HorizontalPropertyId_IsPinned_PublishedAtUtc",
                table: "MuralPosts",
                columns: new[] { "TenantId", "HorizontalPropertyId", "IsPinned", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MuralPosts_TenantId_HorizontalPropertyId_Kind_Category",
                table: "MuralPosts",
                columns: new[] { "TenantId", "HorizontalPropertyId", "Kind", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MuralPosts");
        }
    }
}
