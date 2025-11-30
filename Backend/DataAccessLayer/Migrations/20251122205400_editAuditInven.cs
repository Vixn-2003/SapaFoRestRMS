using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class editAuditInven : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Bước 1: Drop các foreign keys nếu có
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__AuditInventory__CreatorId')
                    ALTER TABLE [AuditInventory] DROP CONSTRAINT [FK__AuditInventory__CreatorId];
                
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__AuditInventory__ConfirmerId')
                    ALTER TABLE [AuditInventory] DROP CONSTRAINT [FK__AuditInventory__ConfirmerId];
            ");

            // ✅ Bước 2: Drop các indexes liên quan đến AuditId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK__AuditInventory__AuditId')
                    ALTER TABLE [AuditInventory] DROP CONSTRAINT [PK__AuditInventory__AuditId];
            ");

            // ✅ Bước 3: Drop column AuditId cũ
            migrationBuilder.DropColumn(
                name: "AuditId",
                table: "AuditInventory");

            // ✅ Bước 4: Thêm column AuditId mới với type string
            migrationBuilder.AddColumn<string>(
                name: "AuditId",
                table: "AuditInventory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // ✅ Bước 5: Tạo lại primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK__AuditInventory__AuditId",
                table: "AuditInventory",
                column: "AuditId");

            // ✅ Bước 6: Tạo lại foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK__AuditInventory__CreatorId",
                table: "AuditInventory",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__AuditInventory__ConfirmerId",
                table: "AuditInventory",
                column: "ConfirmerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ✅ Rollback: Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK__AuditInventory__CreatorId",
                table: "AuditInventory");

            migrationBuilder.DropForeignKey(
                name: "FK__AuditInventory__ConfirmerId",
                table: "AuditInventory");

            // ✅ Drop primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK__AuditInventory__AuditId",
                table: "AuditInventory");

            // ✅ Drop column string AuditId
            migrationBuilder.DropColumn(
                name: "AuditId",
                table: "AuditInventory");

            // ✅ Thêm lại column int AuditId với IDENTITY
            migrationBuilder.AddColumn<int>(
                name: "AuditId",
                table: "AuditInventory",
                type: "int",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            // ✅ Tạo lại primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK__AuditInventory__AuditId",
                table: "AuditInventory",
                column: "AuditId");

            // ✅ Tạo lại foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK__AuditInventory__CreatorId",
                table: "AuditInventory",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK__AuditInventory__ConfirmerId",
                table: "AuditInventory",
                column: "ConfirmerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}