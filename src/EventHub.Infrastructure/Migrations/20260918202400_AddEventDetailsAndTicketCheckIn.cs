using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDetailsAndTicketCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckedInById",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrTokenHash",
                table: "Tickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Events",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Events",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Events",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterImageUrl",
                table: "Events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_QrTokenHash",
                table: "Tickets",
                column: "QrTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_QrTokenHash",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CheckedInById",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "QrTokenHash",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PosterImageUrl",
                table: "Events");
        }
    }
}
