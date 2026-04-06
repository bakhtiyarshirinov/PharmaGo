using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationPolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PickupAvailableFromUtc",
                schema: "public",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryKey",
                schema: "public",
                table: "notification_delivery_logs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_UserId_ReservationId_Channel_Del~",
                schema: "public",
                table: "notification_delivery_logs",
                columns: new[] { "UserId", "ReservationId", "Channel", "DeliveryKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_logs_UserId_ReservationId_Channel_Del~",
                schema: "public",
                table: "notification_delivery_logs");

            migrationBuilder.DropColumn(
                name: "PickupAvailableFromUtc",
                schema: "public",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "DeliveryKey",
                schema: "public",
                table: "notification_delivery_logs");
        }
    }
}
