using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoucherUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__Vouchers__A25C5AA74763DC38",
                table: "Vouchers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Vouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Vouchers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Vouchers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Vouchers_Code_StartDate_EndDate",
                table: "Vouchers",
                columns: new[] { "Code", "StartDate", "EndDate" },
                unique: true,
                filter: "[StartDate] IS NOT NULL AND [EndDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Vouchers_Code_StartDate_EndDate",
                table: "Vouchers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Vouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "Vouchers",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "Vouchers",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Vouchers__A25C5AA74763DC38",
                table: "Vouchers",
                column: "Code",
                unique: true);
        }
    }
}
