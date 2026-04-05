using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservation_idempotency_records",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_idempotency_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservation_idempotency_records_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "public",
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reservation_idempotency_records_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reservation_idempotency_records_ReservationId",
                schema: "public",
                table: "reservation_idempotency_records",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_idempotency_records_UserId_IdempotencyKey",
                schema: "public",
                table: "reservation_idempotency_records",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_idempotency_records",
                schema: "public");
        }
    }
}
