using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Property.Infrastructure.Persistence;

#nullable disable

namespace Property.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260823180000_AddMuralPostAudience")]
    public partial class AddMuralPostAudience : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudienceBuildingIdsJson",
                table: "MuralPosts",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "AudienceFloorIdsJson",
                table: "MuralPosts",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "AudienceOccupancyRolesJson",
                table: "MuralPosts",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudienceBuildingIdsJson",
                table: "MuralPosts");

            migrationBuilder.DropColumn(
                name: "AudienceFloorIdsJson",
                table: "MuralPosts");

            migrationBuilder.DropColumn(
                name: "AudienceOccupancyRolesJson",
                table: "MuralPosts");
        }
    }
}
