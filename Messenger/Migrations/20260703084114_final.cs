using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenTime",
                table: "ChatMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenTime",
                table: "ChatMembers",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
