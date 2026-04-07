using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budgeteria.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "Members",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteTokenExpiry",
                table: "Members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_InviteToken",
                table: "Members",
                column: "InviteToken",
                unique: true,
                filter: "\"InviteToken\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_InviteToken",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "InviteTokenExpiry",
                table: "Members");
        }
    }
}
