using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ClosingBalance",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosingDenominations",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverNotes",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HandoverTime",
                table: "Shifts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandoverToStaffId",
                table: "Shifts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningDenominations",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_HandoverToStaffId",
                table: "Shifts",
                column: "HandoverToStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Staffs_HandoverToStaffId",
                table: "Shifts",
                column: "HandoverToStaffId",
                principalTable: "Staffs",
                principalColumn: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Staffs_HandoverToStaffId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_HandoverToStaffId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ClosingBalance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ClosingDenominations",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverNotes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverTime",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverToStaffId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "OpeningDenominations",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Shifts");
        }
    }
}
