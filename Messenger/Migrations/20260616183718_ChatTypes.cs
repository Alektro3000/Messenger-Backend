using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messenger.Migrations
{
    /// <inheritdoc />
    public partial class ChatTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarUri",
                table: "Users",
                newName: "AvatarUrl");

            migrationBuilder.RenameColumn(
                name: "AvatarUri",
                table: "Chats",
                newName: "AvatarUrl");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Chats",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Chats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Chats");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "Users",
                newName: "AvatarUri");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "Chats",
                newName: "AvatarUri");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Chats",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
