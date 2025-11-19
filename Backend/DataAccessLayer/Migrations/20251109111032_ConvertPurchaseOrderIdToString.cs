using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPurchaseOrderIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK__PurchaseO__Purch__3493CFA7",
                table: "PurchaseOrderDetails");

            // 2. Drop index nếu có
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseOrderDetails_PurchaseOrderId' AND object_id = OBJECT_ID('PurchaseOrderDetails'))
                DROP INDEX IX_PurchaseOrderDetails_PurchaseOrderId ON PurchaseOrderDetails
            ");

            // 3. Add temporary column for new string ID
            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderId_New",
                table: "PurchaseOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderId_New",
                table: "PurchaseOrderDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // 4. Copy data from old column to new column with format
            migrationBuilder.Sql(@"
                UPDATE PurchaseOrders 
                SET PurchaseOrderId_New = 'PO-' + RIGHT('00000000' + CAST(PurchaseOrderId AS VARCHAR), 8)
            ");

            migrationBuilder.Sql(@"
                UPDATE PurchaseOrderDetails 
                SET PurchaseOrderId_New = 'PO-' + RIGHT('00000000' + CAST(PurchaseOrderId AS VARCHAR), 8)
            ");

            // 5. Drop old primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__036BACA49E3BAAAB",
                table: "PurchaseOrders");

            // 6. Drop old columns
            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "PurchaseOrderDetails");

            // 7. Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId_New",
                table: "PurchaseOrders",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId_New",
                table: "PurchaseOrderDetails",
                newName: "PurchaseOrderId");

            // 8. Add new primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__036BACA49E3BAAAB",
                table: "PurchaseOrders",
                column: "PurchaseOrderId");

            // 9. Create index on foreign key column
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId");

            // 10. Recreate foreign key
            migrationBuilder.AddForeignKey(
                name: "FK__PurchaseO__Purch__3493CFA7",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "PurchaseOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK__PurchaseO__Purch__3493CFA7",
                table: "PurchaseOrderDetails");

            // 2. Drop index
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails");

            // 3. Add temporary int columns
            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId_Old",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId_Old",
                table: "PurchaseOrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 4. Convert string back to int (extract number from 'PO-00000001')
            migrationBuilder.Sql(@"
                UPDATE PurchaseOrders 
                SET PurchaseOrderId_Old = CAST(SUBSTRING(PurchaseOrderId, 4, LEN(PurchaseOrderId)) AS INT)
            ");

            migrationBuilder.Sql(@"
                UPDATE PurchaseOrderDetails 
                SET PurchaseOrderId_Old = CAST(SUBSTRING(PurchaseOrderId, 4, LEN(PurchaseOrderId)) AS INT)
            ");

            // 5. Drop primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__036BACA49E3BAAAB",
                table: "PurchaseOrders");

            // 6. Drop string columns
            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "PurchaseOrderDetails");

            // 7. Rename back
            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId_Old",
                table: "PurchaseOrders",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId_Old",
                table: "PurchaseOrderDetails",
                newName: "PurchaseOrderId");

            // 8. Recreate primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__036BACA49E3BAAAB",
                table: "PurchaseOrders",
                column: "PurchaseOrderId");

            // 9. Create index
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId");

            // 10. Recreate foreign key
            migrationBuilder.AddForeignKey(
                name: "FK__PurchaseO__Purch__3493CFA7",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "PurchaseOrderId");
        }
    }
}
