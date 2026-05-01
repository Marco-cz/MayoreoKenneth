using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MayoreoKenneth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSyncStoreSettingsAndProductPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "Suppliers",
                comment: "Proveedores. Retención: no borrar filas salvo baja contractual; auditoría comercial.");

            migrationBuilder.AlterTable(
                name: "ProductSupplierMaps",
                comment: "Mapeo a SKU proveedor y stock/costo conocidos. Retención: vinculada al producto; sin purge suelta.");

            migrationBuilder.AlterTable(
                name: "Products",
                comment: "Catálogo propio. Retención: productos con borrado lógico (IsCatalogHidden); no purge automático de filas.");

            migrationBuilder.AlterTable(
                name: "PriceRules",
                comment: "Reglas históricas de precio. Retención: opcional purga >24 meses si política comercial lo permite (job futuro).");

            migrationBuilder.AlterTable(
                name: "Orders",
                comment: "Órdenes de venta. Retención fiscal CR: conservar años (típico 7+); NO borrar con job sin criterio legal.");

            migrationBuilder.AlterTable(
                name: "OrderItems",
                comment: "Líneas de orden. Retención: alineada a Orders; sin borrado automático.");

            migrationBuilder.AddColumn<int>(
                name: "LastKnownStockQuantity",
                table: "ProductSupplierMaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AdminDiscountPercent",
                table: "Products",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DisplayPriceWithIva",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstOutOfStockAtUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCatalogHidden",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IvaIncludedInDisplayPrice",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CatalogSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    RowsAdded = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RowsHidden = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSyncLogs", x => x.Id);
                },
                comment: "Historial de sync de catálogo. Retención operativa: 90 días (DataRetentionHostedService).");

            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultMarkupPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    IvaPercentOnSale = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    UnavailableHideAfterDays = table.Column<int>(type: "integer", nullable: false),
                    CatalogSyncHourLocal = table.Column<int>(type: "integer", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LastCatalogSyncCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.Id);
                },
                comment: "Configuración global (margen, IVA, días sin stock). Retención: fila única permanente.");

            migrationBuilder.InsertData(
                table: "StoreSettings",
                columns: new[] { "Id", "CatalogSyncHourLocal", "DefaultMarkupPercent", "IvaPercentOnSale", "LastCatalogSyncCompletedAtUtc", "TimeZoneId", "UnavailableHideAfterDays", "UpdatedAtUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), 2, 40m, 13m, null, "America/Costa_Rica", 21, new DateTime(2026, 5, 1, 1, 51, 14, 254, DateTimeKind.Utc).AddTicks(3799) });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "ApiBaseUrl", "Code", "CreatedAtUtc", "IsActive", "Name", "UpdatedAtUtc" },
                values: new object[] { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), null, "SIM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Proveedor simulado", null });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSyncLogs_StartedAtUtc",
                table: "CatalogSyncLogs",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogSyncLogs");

            migrationBuilder.DropTable(
                name: "StoreSettings");

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DropColumn(
                name: "LastKnownStockQuantity",
                table: "ProductSupplierMaps");

            migrationBuilder.DropColumn(
                name: "AdminDiscountPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayPriceWithIva",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FirstOutOfStockAtUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsCatalogHidden",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IvaIncludedInDisplayPrice",
                table: "Products");

            migrationBuilder.AlterTable(
                name: "Suppliers",
                oldComment: "Proveedores. Retención: no borrar filas salvo baja contractual; auditoría comercial.");

            migrationBuilder.AlterTable(
                name: "ProductSupplierMaps",
                oldComment: "Mapeo a SKU proveedor y stock/costo conocidos. Retención: vinculada al producto; sin purge suelta.");

            migrationBuilder.AlterTable(
                name: "Products",
                oldComment: "Catálogo propio. Retención: productos con borrado lógico (IsCatalogHidden); no purge automático de filas.");

            migrationBuilder.AlterTable(
                name: "PriceRules",
                oldComment: "Reglas históricas de precio. Retención: opcional purga >24 meses si política comercial lo permite (job futuro).");

            migrationBuilder.AlterTable(
                name: "Orders",
                oldComment: "Órdenes de venta. Retención fiscal CR: conservar años (típico 7+); NO borrar con job sin criterio legal.");

            migrationBuilder.AlterTable(
                name: "OrderItems",
                oldComment: "Líneas de orden. Retención: alineada a Orders; sin borrado automático.");
        }
    }
}
