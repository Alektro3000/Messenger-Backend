using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class DirectChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DirectUser1Id",
                table: "Chats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectUser2Id",
                table: "Chats",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_DirectUser1Id_DirectUser2Id",
                table: "Chats",
                columns: new[] { "DirectUser1Id", "DirectUser2Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_DirectUser1Id_DirectUser2Id",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "DirectUser1Id",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "DirectUser2Id",
                table: "Chats");
        }
    }
}
