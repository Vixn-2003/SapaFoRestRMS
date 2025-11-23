using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderHistoryAndUndoConfirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Orders",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByStaffId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderHistories",
                columns: table => new
                {
                    OrderHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHistories", x => x.OrderHistoryId);
                    table.ForeignKey(
                        name: "FK_OrderHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderHistories_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ConfirmedByStaffId",
                table: "Orders",
                column: "ConfirmedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistories_OrderId",
                table: "OrderHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistories_StaffId",
                table: "OrderHistories",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "ComboId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Staffs_ConfirmedByStaffId",
                table: "Orders",
                column: "ConfirmedByStaffId",
                principalTable: "Staffs",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Staffs_ConfirmedByStaffId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ConfirmedByStaffId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConfirmedByStaffId",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "ComboId");
        }
    }
}
