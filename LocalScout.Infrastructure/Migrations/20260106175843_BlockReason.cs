using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalScout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BlockReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "AspNetUsers");
        }
    }
}
