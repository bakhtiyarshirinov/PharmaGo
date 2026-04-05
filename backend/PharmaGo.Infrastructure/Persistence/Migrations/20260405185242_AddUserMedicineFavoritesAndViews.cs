using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMedicineFavoritesAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_favorite_medicines",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_medicines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_favorite_medicines_medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "public",
                        principalTable: "medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_favorite_medicines_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_medicine_views",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastViewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_medicine_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_medicine_views_medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalSchema: "public",
                        principalTable: "medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_medicine_views_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_medicines_MedicineId",
                schema: "public",
                table: "user_favorite_medicines",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_medicines_UserId_CreatedAtUtc",
                schema: "public",
                table: "user_favorite_medicines",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_medicines_UserId_MedicineId",
                schema: "public",
                table: "user_favorite_medicines",
                columns: new[] { "UserId", "MedicineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_medicine_views_MedicineId",
                schema: "public",
                table: "user_medicine_views",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_user_medicine_views_UserId_LastViewedAtUtc",
                schema: "public",
                table: "user_medicine_views",
                columns: new[] { "UserId", "LastViewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_medicine_views_UserId_MedicineId",
                schema: "public",
                table: "user_medicine_views",
                columns: new[] { "UserId", "MedicineId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_favorite_medicines",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_medicine_views",
                schema: "public");
        }
    }
}
