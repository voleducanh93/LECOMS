using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFielSellerRefundResponseDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellerRefundResponseHours",
                table: "PlatformConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PlatformConfigs",
                keyColumn: "Id",
                keyValue: "PLATFORM_CONFIG_SINGLETON",
                column: "SellerRefundResponseHours",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerRefundResponseHours",
                table: "PlatformConfigs");
        }
    }
}
