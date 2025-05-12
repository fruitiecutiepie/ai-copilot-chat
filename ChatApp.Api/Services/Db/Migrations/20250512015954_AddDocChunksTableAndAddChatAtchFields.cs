using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFileNameAndFileTypeToChatMsgAtchs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ChatMessageAttachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FileType",
                table: "ChatMessageAttachments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ChatMessageAttachments");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "ChatMessageAttachments");
        }
    }
}
