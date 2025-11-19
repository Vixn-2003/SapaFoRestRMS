using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddComboToOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MenuItemId",
                table: "OrderDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ComboId",
                table: "OrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ComboId",
                table: "OrderDetails",
                column: "ComboId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "ComboId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_ComboId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ComboId",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<int>(
                name: "MenuItemId",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
