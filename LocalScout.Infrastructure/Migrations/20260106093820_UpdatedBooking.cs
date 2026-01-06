using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalScout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoCancelWarningCount",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedEndDateTime",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedStartDateTime",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RequestedEndTime",
                table: "Bookings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RequestedStartTime",
                table: "Bookings",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderTimeSlots",
                columns: table => new
                {
                    TimeSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderTimeSlots", x => x.TimeSlotId);
                });

            migrationBuilder.CreateTable(
                name: "RescheduleProposals",
                columns: table => new
                {
                    ProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProposedStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ProposedEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescheduleProposals", x => x.ProposalId);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBlocks",
                columns: table => new
                {
                    ServiceBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnblockAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBlocks", x => x.ServiceBlockId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderTimeSlots");

            migrationBuilder.DropTable(
                name: "RescheduleProposals");

            migrationBuilder.DropTable(
                name: "ServiceBlocks");

            migrationBuilder.DropColumn(
                name: "AutoCancelWarningCount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedEndDateTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedStartDateTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RequestedDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RequestedEndTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RequestedStartTime",
                table: "Bookings");
        }
    }
}
