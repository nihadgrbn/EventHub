using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketRefundStatusAndPaymentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PaymentId",
                table: "Purchases",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Payments_PaymentId",
                table: "Purchases",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Payments_PaymentId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PaymentId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Purchases");
        }
    }
}
