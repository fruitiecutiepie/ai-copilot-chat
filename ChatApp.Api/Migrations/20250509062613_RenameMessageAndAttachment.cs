using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameMessageAndAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageAttachments_Messages_MessageId",
                table: "MessageAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageAttachments",
                table: "MessageAttachments");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "ChatMessages");

            migrationBuilder.RenameTable(
                name: "MessageAttachments",
                newName: "ChatMessageAttachments");

            migrationBuilder.RenameIndex(
                name: "IX_MessageAttachments_MessageId",
                table: "ChatMessageAttachments",
                newName: "IX_ChatMessageAttachments_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessageAttachments",
                table: "ChatMessageAttachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageAttachments_ChatMessages_MessageId",
                table: "ChatMessageAttachments",
                column: "MessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageAttachments_ChatMessages_MessageId",
                table: "ChatMessageAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessageAttachments",
                table: "ChatMessageAttachments");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "ChatMessageAttachments",
                newName: "MessageAttachments");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessageAttachments_MessageId",
                table: "MessageAttachments",
                newName: "IX_MessageAttachments_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageAttachments",
                table: "MessageAttachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageAttachments_Messages_MessageId",
                table: "MessageAttachments",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
