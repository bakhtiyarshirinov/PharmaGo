using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_delivery_logs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_logs_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "public",
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notification_delivery_logs_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TelegramEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationConfirmedEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationReadyEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationCancelledEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationExpiredEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationExpiringSoonEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_ReservationId_EventType_Channel",
                schema: "public",
                table: "notification_delivery_logs",
                columns: new[] { "ReservationId", "EventType", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_logs_UserId_EventType_Channel_Created~",
                schema: "public",
                table: "notification_delivery_logs",
                columns: new[] { "UserId", "EventType", "Channel", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_UserId",
                schema: "public",
                table: "notification_preferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_delivery_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "public");
        }
    }
}
