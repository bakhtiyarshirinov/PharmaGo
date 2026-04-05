using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPharmacyFavoritesAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_favorite_pharmacies",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_pharmacies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_favorite_pharmacies_pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalSchema: "public",
                        principalTable: "pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_favorite_pharmacies_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_pharmacy_views",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastViewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_pharmacy_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_pharmacy_views_pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalSchema: "public",
                        principalTable: "pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_pharmacy_views_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_pharmacies_PharmacyId",
                schema: "public",
                table: "user_favorite_pharmacies",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_pharmacies_UserId_CreatedAtUtc",
                schema: "public",
                table: "user_favorite_pharmacies",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_pharmacies_UserId_PharmacyId",
                schema: "public",
                table: "user_favorite_pharmacies",
                columns: new[] { "UserId", "PharmacyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_pharmacy_views_PharmacyId",
                schema: "public",
                table: "user_pharmacy_views",
                column: "PharmacyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_pharmacy_views_UserId_LastViewedAtUtc",
                schema: "public",
                table: "user_pharmacy_views",
                columns: new[] { "UserId", "LastViewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_pharmacy_views_UserId_PharmacyId",
                schema: "public",
                table: "user_pharmacy_views",
                columns: new[] { "UserId", "PharmacyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_favorite_pharmacies",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_pharmacy_views",
                schema: "public");
        }
    }
}
