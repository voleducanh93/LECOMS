using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherAndTransactionAndWalletFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Vouchers");

            migrationBuilder.RenameColumn(
                name: "MaxUsagePerUser",
                table: "Vouchers",
                newName: "UsageLimitPerUser");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Vouchers",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Vouchers",
                newName: "MinOrderAmount");

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Vouchers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Vouchers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityAvailable",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Vouchers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "UserVouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "UserVouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherCodeUsed",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "QuantityAvailable",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "VoucherCodeUsed",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "UsageLimitPerUser",
                table: "Vouchers",
                newName: "MaxUsagePerUser");

            migrationBuilder.RenameColumn(
                name: "MinOrderAmount",
                table: "Vouchers",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Vouchers",
                newName: "ExpiresAt");

            migrationBuilder.AddColumn<byte>(
                name: "Active",
                table: "Vouchers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "Vouchers",
                type: "int",
                nullable: true);
        }
    }
}
