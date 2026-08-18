using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class MessageHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteTime",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditTime",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    SendTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageHistories", x => new { x.Id, x.Version });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageHistories");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeleteTime",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "EditTime",
                table: "Messages");
        }
    }
}
