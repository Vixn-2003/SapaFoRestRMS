using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorAndConfirmerToPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeSupplier",
                table: "Suppliers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdConfirm",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdCreator",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IdConfirm",
                table: "PurchaseOrders",
                column: "IdConfirm");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IdCreator",
                table: "PurchaseOrders",
                column: "IdCreator");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_Confirmer",
                table: "PurchaseOrders",
                column: "IdConfirm",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_Creator",
                table: "PurchaseOrders",
                column: "IdCreator",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_Confirmer",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_Creator",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_IdConfirm",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_IdCreator",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CodeSupplier",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IdConfirm",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IdCreator",
                table: "PurchaseOrders");
        }
    }
}
