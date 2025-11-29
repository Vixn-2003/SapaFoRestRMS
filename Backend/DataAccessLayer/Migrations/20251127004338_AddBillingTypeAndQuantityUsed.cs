using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingTypeAndQuantityUsed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add BillingType column to MenuItems if not exists (safe SQL)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'MenuItems' AND COLUMN_NAME = 'BillingType'
                )
                BEGIN
                    ALTER TABLE MenuItems ADD BillingType INT NOT NULL DEFAULT 2;
                END
            ");

            // Add QuantityUsed column to OrderDetails if not exists (safe SQL)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'OrderDetails' AND COLUMN_NAME = 'QuantityUsed'
                )
                BEGIN
                    ALTER TABLE OrderDetails ADD QuantityUsed INT NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove QuantityUsed column
            migrationBuilder.DropColumn(
                name: "QuantityUsed",
                table: "OrderDetails");

            // Remove BillingType column
            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "MenuItems");
        }
    }
}
