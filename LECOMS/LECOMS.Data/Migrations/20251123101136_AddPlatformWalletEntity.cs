using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformWalletEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformWallets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommissionEarned = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommissionRefunded = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlatformWalletId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformWalletTransactions_PlatformWallets_PlatformWalletId",
                        column: x => x.PlatformWalletId,
                        principalTable: "PlatformWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformWalletTransactions_CreatedAt",
                table: "PlatformWalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformWalletTransactions_PlatformWalletId",
                table: "PlatformWalletTransactions",
                column: "PlatformWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformWalletTransactions_ReferenceId_ReferenceType",
                table: "PlatformWalletTransactions",
                columns: new[] { "ReferenceId", "ReferenceType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformWalletTransactions");

            migrationBuilder.DropTable(
                name: "PlatformWallets");
        }
    }
}
