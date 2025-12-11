using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalScout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewPropertiesInUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "AspNetUsers");
        }
    }
}
