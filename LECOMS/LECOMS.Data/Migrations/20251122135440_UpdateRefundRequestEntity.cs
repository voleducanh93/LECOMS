using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefundRequestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "RefundRequests",
                newName: "RefundedAt");

            migrationBuilder.AlterColumn<string>(
                name: "ShopRejectReason",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessNote",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrls",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminRejectReason",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminRespondedAt",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminResponseBy",
                table: "RefundRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundTransactionId",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_AdminResponseBy",
                table: "RefundRequests",
                column: "AdminResponseBy");

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_AspNetUsers_AdminResponseBy",
                table: "RefundRequests",
                column: "AdminResponseBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_AspNetUsers_AdminResponseBy",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_AdminResponseBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "AdminRejectReason",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "AdminRespondedAt",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "AdminResponseBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "RefundTransactionId",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "RefundedAt",
                table: "RefundRequests",
                newName: "ProcessedAt");

            migrationBuilder.AlterColumn<string>(
                name: "ShopRejectReason",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessNote",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrls",
                table: "RefundRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "RefundRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "RefundRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
