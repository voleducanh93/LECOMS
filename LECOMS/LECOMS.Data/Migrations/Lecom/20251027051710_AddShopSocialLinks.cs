using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations.Lecom
{
    /// <inheritdoc />
    public partial class AddShopSocialLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShopFacebook",
                table: "Shops",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopInstagram",
                table: "Shops",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopTiktok",
                table: "Shops",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopFacebook",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopInstagram",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopTiktok",
                table: "Shops");
        }
    }
}
