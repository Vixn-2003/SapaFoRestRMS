using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "MarketingCampaigns",
                newName: "ImageUrl");

            migrationBuilder.AddColumn<decimal>(
                name: "Budget",
                table: "MarketingCampaigns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CampaignType",
                table: "MarketingCampaigns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RevenueGenerated",
                table: "MarketingCampaigns",
                type: "decimal(18,2)",
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TargetAudience",
                table: "MarketingCampaigns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetReach",
                table: "MarketingCampaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetRevenue",
                table: "MarketingCampaigns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "MarketingCampaigns",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VoucherId",
                table: "MarketingCampaigns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_VoucherId",
                table: "MarketingCampaigns",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarketingCampaigns_Vouchers",
                table: "MarketingCampaigns",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarketingCampaigns_Vouchers",
                table: "MarketingCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_MarketingCampaigns_VoucherId",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "Budget",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "CampaignType",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "RevenueGenerated",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetReach",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "TargetRevenue",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "MarketingCampaigns");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "MarketingCampaigns");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "MarketingCampaigns",
                newName: "Description");
        }
    }
}
