using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverySchemaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_items_MedicineId",
                schema: "public",
                table: "stock_items");

            migrationBuilder.AddColumn<bool>(
                name: "IsReservable",
                schema: "public",
                table: "stock_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStockUpdatedAtUtc",
                schema: "public",
                table: "stock_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDelivery",
                schema: "public",
                table: "pharmacies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationVerifiedAtUtc",
                schema: "public",
                table: "pharmacies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLatitude",
                schema: "public",
                table: "pharmacies",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLongitude",
                schema: "public",
                table: "pharmacies",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningHoursJson",
                schema: "public",
                table: "pharmacies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsReservations",
                schema: "public",
                table: "pharmacies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE public.pharmacies
                SET "LocationLatitude" = CASE
                        WHEN "Latitude" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN CAST("Latitude" AS numeric(9,6))
                        ELSE "LocationLatitude"
                    END,
                    "LocationLongitude" = CASE
                        WHEN "Longitude" ~ '^-?[0-9]+(\.[0-9]+)?$' THEN CAST("Longitude" AS numeric(9,6))
                        ELSE "LocationLongitude"
                    END,
                    "SupportsReservations" = TRUE,
                    "LastLocationVerifiedAtUtc" = COALESCE("LastLocationVerifiedAtUtc", NOW());
                """);

            migrationBuilder.Sql("""
                UPDATE public.stock_items
                SET "IsReservable" = TRUE,
                    "LastStockUpdatedAtUtc" = COALESCE("LastStockUpdatedAtUtc", NOW());
                """);

            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_medicines_brand_name_trgm"
                ON public.medicines
                USING gin ("BrandName" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_medicines_generic_name_trgm"
                ON public.medicines
                USING gin ("GenericName" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_medicines_barcode_trgm"
                ON public.medicines
                USING gin ("Barcode" gin_trgm_ops);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_MedicineId_PharmacyId_IsActive_IsReservable_Exp~",
                schema: "public",
                table: "stock_items",
                columns: new[] { "MedicineId", "PharmacyId", "IsActive", "IsReservable", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_PharmacyId_IsActive_ExpirationDate",
                schema: "public",
                table: "stock_items",
                columns: new[] { "PharmacyId", "IsActive", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_pharmacies_IsActive_LocationLatitude_LocationLongitude",
                schema: "public",
                table: "pharmacies",
                columns: new[] { "IsActive", "LocationLatitude", "LocationLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_pharmacies_IsActive_SupportsReservations",
                schema: "public",
                table: "pharmacies",
                columns: new[] { "IsActive", "SupportsReservations" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS public."IX_medicines_barcode_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS public."IX_medicines_generic_name_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS public."IX_medicines_brand_name_trgm";""");

            migrationBuilder.DropIndex(
                name: "IX_stock_items_MedicineId_PharmacyId_IsActive_IsReservable_Exp~",
                schema: "public",
                table: "stock_items");

            migrationBuilder.DropIndex(
                name: "IX_stock_items_PharmacyId_IsActive_ExpirationDate",
                schema: "public",
                table: "stock_items");

            migrationBuilder.DropIndex(
                name: "IX_pharmacies_IsActive_LocationLatitude_LocationLongitude",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_pharmacies_IsActive_SupportsReservations",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "IsReservable",
                schema: "public",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "LastStockUpdatedAtUtc",
                schema: "public",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "HasDelivery",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "LastLocationVerifiedAtUtc",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "OpeningHoursJson",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.DropColumn(
                name: "SupportsReservations",
                schema: "public",
                table: "pharmacies");

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_MedicineId",
                schema: "public",
                table: "stock_items",
                column: "MedicineId");
        }
    }
}
