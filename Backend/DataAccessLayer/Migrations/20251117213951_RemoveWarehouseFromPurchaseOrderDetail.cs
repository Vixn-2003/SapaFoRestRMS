using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarehouseFromPurchaseOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Inventory__Wareh__WarehouseId",
                table: "InventoryBatches");

            migrationBuilder.AddColumn<string>(
                name: "WarehouseName",
                table: "PurchaseOrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryBatch_Warehouses",
                table: "InventoryBatches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryBatch_Warehouses",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "WarehouseName",
                table: "PurchaseOrderDetails");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "PurchaseOrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_WarehouseId",
                table: "PurchaseOrderDetails",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK__Inventory__Wareh__WarehouseId",
                table: "InventoryBatches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_Warehouses",
                table: "PurchaseOrderDetails",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
