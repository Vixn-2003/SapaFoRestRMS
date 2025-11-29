using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditInventoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditInventory",
                columns: table => new
                {
                    AuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IngredientCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    CreatorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatorPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatorPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustmentQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsAddition = table.Column<bool>(type: "bit", nullable: false),
                    IngredientStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuditStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "processing"),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmerId = table.Column<int>(type: "int", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ConfirmerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmerPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfirmerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditInventory__AuditId", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK__AuditInventory__ConfirmerId",
                        column: x => x.ConfirmerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK__AuditInventory__CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_AuditStatus",
                table: "AuditInventory",
                column: "AuditStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_ConfirmerId",
                table: "AuditInventory",
                column: "ConfirmerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_CreatedAt",
                table: "AuditInventory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_CreatorId",
                table: "AuditInventory",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_IngredientCode",
                table: "AuditInventory",
                column: "IngredientCode");

            migrationBuilder.CreateIndex(
                name: "IX_AuditInventory_PurchaseOrderId",
                table: "AuditInventory",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditInventory");
        }
    }
}
