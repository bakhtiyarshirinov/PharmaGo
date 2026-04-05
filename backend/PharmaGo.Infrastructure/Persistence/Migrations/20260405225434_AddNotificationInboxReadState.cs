using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationInboxReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                schema: "public",
                table: "notification_delivery_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_UserId_Status_ReadAtUtc_CreatedA~",
                schema: "public",
                table: "notification_delivery_logs",
                columns: new[] { "UserId", "Status", "ReadAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_logs_UserId_Status_ReadAtUtc_CreatedA~",
                schema: "public",
                table: "notification_delivery_logs");

            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                schema: "public",
                table: "notification_delivery_logs");
        }
    }
}
