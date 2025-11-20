using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class add_table_warehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tạo bảng Warehouses
            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Warehouse__ID", x => x.WarehouseId);
                });

            // 2. Thêm cột WarehouseId vào InventoryBatches
            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "InventoryBatches",
                type: "int",
                nullable: true);

            // 3. Tạo index
            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_WarehouseId",
                table: "InventoryBatches",
                column: "WarehouseId");

            // 4. Tạo foreign key
            migrationBuilder.AddForeignKey(
                name: "FK__Inventory__Wareh__WarehouseId",
                table: "InventoryBatches",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa foreign key
            migrationBuilder.DropForeignKey(
                name: "FK__Inventory__Wareh__WarehouseId",
                table: "InventoryBatches");

            // 2. Xóa index
            migrationBuilder.DropIndex(
                name: "IX_InventoryBatches_WarehouseId",
                table: "InventoryBatches");

            // 3. Xóa cột
            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "InventoryBatches");

            // 4. Xóa bảng
            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
