using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migrations2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "CommunityPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModeratorNote",
                table: "CommunityPosts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "CommunityPosts");

            migrationBuilder.DropColumn(
                name: "ModeratorNote",
                table: "CommunityPosts");
        }
    }
}
