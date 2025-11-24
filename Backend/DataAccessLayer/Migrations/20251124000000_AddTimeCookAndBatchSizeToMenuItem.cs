using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeCookAndBatchSizeToMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeCook",
                table: "MenuItems",
                type: "int",
                nullable: true,
                comment: "Thời gian nấu (phút)");

            migrationBuilder.AddColumn<int>(
                name: "BatchSize",
                table: "MenuItems",
                type: "int",
                nullable: true,
                defaultValue: 1,
                comment: "Số lượng mỗi mẻ nấu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeCook",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "BatchSize",
                table: "MenuItems");
        }
    }
}

