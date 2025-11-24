using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBatchReservationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add QuantityReserved column
            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReserved",
                table: "InventoryBatches",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Số lượng đã được bếp phó dành riêng");

            // Add Available as computed column (calculated from QuantityRemaining - QuantityReserved)
            migrationBuilder.AddColumn<decimal>(
                name: "Available",
                table: "InventoryBatches",
                type: "decimal(18, 2)",
                nullable: false,
                computedColumnSql: "([QuantityRemaining] - [QuantityReserved])",
                stored: true,
                comment: "Số lượng khả dụng (computed)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Available",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "QuantityReserved",
                table: "InventoryBatches");
        }
    }
}

