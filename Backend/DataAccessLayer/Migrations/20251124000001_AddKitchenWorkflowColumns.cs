using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenWorkflowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to OrderDetails table
            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "OrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Đánh dấu order được yêu cầu làm ngay");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                table: "OrderDetails",
                type: "datetime",
                nullable: true,
                comment: "Thời gian món được đánh dấu Sẵn sàng");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "OrderDetails",
                type: "datetime",
                nullable: true,
                comment: "Thời gian bắt đầu nấu");

            // Modify existing CreatedAt column to have default value
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OrderDetails",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            // Modify existing Notes column to have max length
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "OrderDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Add columns to KitchenTicketDetails table
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "KitchenTicketDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                comment: "Status: Pending, Cooking, Done");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "KitchenTicketDetails",
                type: "datetime",
                nullable: true,
                comment: "When staff started cooking");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "KitchenTicketDetails",
                type: "datetime",
                nullable: true,
                comment: "When staff completed cooking");

            migrationBuilder.AddColumn<int>(
                name: "AssignedUserId",
                table: "KitchenTicketDetails",
                type: "int",
                nullable: true,
                comment: "Which user is assigned to cook");

            // Create foreign key for AssignedUserId
            migrationBuilder.CreateIndex(
                name: "IX_KitchenTicketDetails_AssignedUserId",
                table: "KitchenTicketDetails",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenTicketDetails_Users_AssignedUserId",
                table: "KitchenTicketDetails",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenTicketDetails_Users_AssignedUserId",
                table: "KitchenTicketDetails");

            migrationBuilder.DropIndex(
                name: "IX_KitchenTicketDetails_AssignedUserId",
                table: "KitchenTicketDetails");

            // Drop columns from KitchenTicketDetails
            migrationBuilder.DropColumn(
                name: "Status",
                table: "KitchenTicketDetails");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "KitchenTicketDetails");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "KitchenTicketDetails");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "KitchenTicketDetails");

            // Drop columns from OrderDetails
            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "OrderDetails");

            // Revert CreatedAt and Notes columns
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}

