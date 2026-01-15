using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalScout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Reschedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProposedBy",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedEndDateTime",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedNotes",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProposedPrice",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedStartDateTime",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedBy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProposedEndDateTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProposedNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProposedPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProposedStartDateTime",
                table: "Bookings");
        }
    }
}
