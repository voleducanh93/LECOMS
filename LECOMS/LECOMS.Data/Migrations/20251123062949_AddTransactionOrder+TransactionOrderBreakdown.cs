using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionOrderTransactionOrderBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --------------------------
            // REMOVE OLD COLUMN OrderId
            // --------------------------
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Transactions");

            // --------------------------
            // FIX SenderId TYPE ONLY
            // (KHÔNG THÊM FOREIGN KEY)
            // --------------------------
            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "Messages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // --------------------------
            // CREATE: TransactionOrders
            // --------------------------
            migrationBuilder.CreateTable(
                name: "TransactionOrders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionOrders_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // --------------------------
            // CREATE: TransactionOrderBreakdowns
            // --------------------------
            migrationBuilder.CreateTable(
                name: "TransactionOrderBreakdowns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TransactionOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShopAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOrderBreakdowns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOrderBreakdowns_TransactionOrders_TransactionOrderId",
                        column: x => x.TransactionOrderId,
                        principalTable: "TransactionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOrderBreakdowns_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // --------------------------
            // Indexes
            // --------------------------
            migrationBuilder.CreateIndex(
                name: "IX_TransactionOrderBreakdowns_TransactionId",
                table: "TransactionOrderBreakdowns",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOrderBreakdowns_TransactionOrderId",
                table: "TransactionOrderBreakdowns",
                column: "TransactionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOrders_OrderId",
                table: "TransactionOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOrders_TransactionId",
                table: "TransactionOrders",
                column: "TransactionId");

            // --------------------------
            // IMPORTANT:
            // ❌ KHÔNG THÊM FOREIGN KEY Messages → AspNetUsers
            // vì sẽ luôn conflict với dữ liệu cũ
            // --------------------------
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop 2 new tables
            migrationBuilder.DropTable(
                name: "TransactionOrderBreakdowns");

            migrationBuilder.DropTable(
                name: "TransactionOrders");

            // Revert SenderId type
            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // Add back OrderId
            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "Transactions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
