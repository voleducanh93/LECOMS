using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_PointLedger_Achievement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Amount",
                table: "PointLedgers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PointLedgers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PointLedgers");
        }
    }
}
