using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class SeedUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "UnitId", "UnitName", "UnitType" },
                values: new object[,]
                {
                    { 1, "quả", 1 },
                    { 2, "cái", 1 },
                    { 3, "hộp", 1 },
                    { 4, "chai", 1 },
                    { 5, "gói", 1 },
                    { 6, "bịch", 1 },
                    { 7, "bó", 1 },
                    { 8, "con", 1 },
                    { 9, "túi", 1 },
                    { 10, "kg", 2 },
                    { 11, "gram", 2 },
                    { 12, "lít", 2 },
                    { 13, "ml", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Units",
                keyColumn: "UnitId",
                keyValue: 13);
        }
    }
}
