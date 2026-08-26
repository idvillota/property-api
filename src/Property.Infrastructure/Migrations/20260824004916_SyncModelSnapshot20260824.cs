using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Property.Infrastructure.Migrations
{
    /// <summary>
    /// Snapshot sync only — schema for newer features lives in hand-written
    /// <c>Persistence/Migrations/*</c>. This migration records an up-to-date
    /// <see cref="ApplicationDbContextModelSnapshot"/> so EF Core 9
    /// <c>PendingModelChangesWarning</c> does not block <c>MigrateAsync</c>.
    /// </summary>
    public partial class SyncModelSnapshot20260824 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: DDL already applied (or will be applied) by Persistence/Migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op.
        }
    }
}
