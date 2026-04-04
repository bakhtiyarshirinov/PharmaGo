using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationAndStockConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pgcrypto;""");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                schema: "public",
                table: "stock_items",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                schema: "public",
                table: "reservations",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                schema: "public",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                schema: "public",
                table: "reservations");
        }
    }
}
