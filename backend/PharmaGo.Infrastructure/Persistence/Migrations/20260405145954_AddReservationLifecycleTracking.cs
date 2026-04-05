using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationLifecycleTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                schema: "public",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                schema: "public",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyForPickupAtUtc",
                schema: "public",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                schema: "public",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "ExpiredAtUtc",
                schema: "public",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "ReadyForPickupAtUtc",
                schema: "public",
                table: "reservations");
        }
    }
}
