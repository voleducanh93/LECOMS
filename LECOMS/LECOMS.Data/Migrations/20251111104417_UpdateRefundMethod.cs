using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LECOMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefundMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_AspNetUsers_ProcessedBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "Recipient",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "ProcessedBy",
                table: "RefundRequests",
                newName: "ShopResponseBy");

            migrationBuilder.RenameIndex(
                name: "IX_RefundRequests_ProcessedBy",
                table: "RefundRequests",
                newName: "IX_RefundRequests_ShopResponseBy");

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

            migrationBuilder.AddColumn<string>(
                name: "ShopRejectReason",
                table: "RefundRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShopRespondedAt",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_AspNetUsers_ShopResponseBy",
                table: "RefundRequests",
                column: "ShopResponseBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefundRequests_AspNetUsers_ShopResponseBy",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "ShopRejectReason",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "ShopRespondedAt",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "ShopResponseBy",
                table: "RefundRequests",
                newName: "ProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_RefundRequests_ShopResponseBy",
                table: "RefundRequests",
                newName: "IX_RefundRequests_ProcessedBy");

            migrationBuilder.AddColumn<int>(
                name: "Recipient",
                table: "RefundRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_RefundRequests_AspNetUsers_ProcessedBy",
                table: "RefundRequests",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
