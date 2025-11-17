using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFlowExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountReceived",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayErrorCode",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayErrorMessage",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualConfirmed",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryAt",
                table: "Transactions",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentTransactionId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditLogs__AuditLogId", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK__AuditLogs__UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "OrderLocks",
                columns: table => new
                {
                    OrderLockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    LockedByUserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "Payment in progress"),
                    LockedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderLocks__OrderLockId", x => x.OrderLockId);
                    table.ForeignKey(
                        name: "FK__OrderLocks__LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__OrderLocks__OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ConfirmedByUserId",
                table: "Transactions",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLocks_ExpiresAt",
                table: "OrderLocks",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLocks_LockedByUserId",
                table: "OrderLocks",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLocks_OrderId",
                table: "OrderLocks",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__Transactions__ConfirmedByUserId",
                table: "Transactions",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Transactions__ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId",
                principalTable: "Transactions",
                principalColumn: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Transactions__ConfirmedByUserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK__Transactions__ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "OrderLocks");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ConfirmedByUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AmountReceived",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "GatewayErrorCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "GatewayErrorMessage",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsManualConfirmed",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastRetryAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Transactions");
        }
    }
}
