using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MayoreoKenneth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastKnownStockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastKnownStockQuantity",
                table: "ProductSupplierMaps");

            migrationBuilder.AlterTable(
                name: "ProductSupplierMaps",
                comment: "Mapeo a id/SKU proveedor y último costo sincronizado. Retención: vinculada al producto; sin purge suelta. No persistimos cantidad de stock.",
                oldComment: "Mapeo a SKU proveedor y stock/costo conocidos. Retención: vinculada al producto; sin purge suelta.");

            migrationBuilder.UpdateData(
                table: "StoreSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "UpdatedAtUtc",
                value: new DateTime(2026, 5, 1, 2, 11, 13, 478, DateTimeKind.Utc).AddTicks(3268));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "ProductSupplierMaps",
                comment: "Mapeo a SKU proveedor y stock/costo conocidos. Retención: vinculada al producto; sin purge suelta.",
                oldComment: "Mapeo a id/SKU proveedor y último costo sincronizado. Retención: vinculada al producto; sin purge suelta. No persistimos cantidad de stock.");

            migrationBuilder.AddColumn<int>(
                name: "LastKnownStockQuantity",
                table: "ProductSupplierMaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StoreSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "UpdatedAtUtc",
                value: new DateTime(2026, 5, 1, 1, 51, 14, 254, DateTimeKind.Utc).AddTicks(3799));
        }
    }
}
