using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJwtAuthAndUserCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                schema: "public",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "public",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "public",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "public",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "public",
                table: "users",
                column: "Email");
        }
    }
}
