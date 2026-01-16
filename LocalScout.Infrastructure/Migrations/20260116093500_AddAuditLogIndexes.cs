using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalScout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add index on Timestamp for ORDER BY performance
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            // Add index on Category for filtering and DISTINCT queries
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            // Add index on Action for filtering and DISTINCT queries
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            // Add index on UserId for user-based filtering
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs");
        }
    }
}
