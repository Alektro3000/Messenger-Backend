using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class DisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "Users",
                newName: "Username");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LastMessageId",
                table: "Chats",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId",
                table: "Messages",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId_Id",
                table: "Messages",
                columns: new[] { "ChatId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId",
                table: "Messages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chats_ChatId",
                table: "Messages",
                column: "ChatId",
                principalTable: "Chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chats_ChatId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId_Id",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Chats");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Users",
                newName: "UserName");
        }
    }
}
