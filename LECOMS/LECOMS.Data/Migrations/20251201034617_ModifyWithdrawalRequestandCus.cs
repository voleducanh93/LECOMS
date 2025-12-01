using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyWithdrawalRequestandCus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "CustomerWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "CustomerWithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "CustomerWithdrawalRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "WithdrawalRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "WithdrawalRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "WithdrawalRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "CustomerWithdrawalRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "CustomerWithdrawalRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "CustomerWithdrawalRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
