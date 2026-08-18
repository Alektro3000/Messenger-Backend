using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class KeyFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_Messages_LastReadMessageChatId_LastReadMessageU~",
                table: "ChatMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_LastReadMessageChatId_LastReadMessageUserId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "LastReadMessageChatId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "LastReadMessageUserId",
                table: "ChatMembers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Messages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_DirectUser2Id",
                table: "Chats",
                column: "DirectUser2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastMessageId",
                table: "Chats",
                column: "LastMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_LastReadMessageId",
                table: "ChatMembers",
                column: "LastReadMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_Messages_LastReadMessageId",
                table: "ChatMembers",
                column: "LastReadMessageId",
                principalTable: "Messages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Messages_LastMessageId",
                table: "Chats",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Users_DirectUser1Id",
                table: "Chats",
                column: "DirectUser1Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Users_DirectUser2Id",
                table: "Chats",
                column: "DirectUser2Id",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_Messages_LastReadMessageId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Messages_LastMessageId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Users_DirectUser1Id",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Users_DirectUser2Id",
                table: "Chats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Chats_DirectUser2Id",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_LastMessageId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_LastReadMessageId",
                table: "ChatMembers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Messages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "LastReadMessageChatId",
                table: "ChatMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReadMessageUserId",
                table: "ChatMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                columns: new[] { "ChatId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId",
                table: "Messages",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_LastReadMessageChatId_LastReadMessageUserId",
                table: "ChatMembers",
                columns: new[] { "LastReadMessageChatId", "LastReadMessageUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_Messages_LastReadMessageChatId_LastReadMessageU~",
                table: "ChatMembers",
                columns: new[] { "LastReadMessageChatId", "LastReadMessageUserId" },
                principalTable: "Messages",
                principalColumns: new[] { "ChatId", "UserId" });
        }
    }
}
