using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHelper.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoteProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposalType = table.Column<string>(type: "text", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetEntityName = table.Column<string>(type: "text", nullable: true),
                    NewEntityName = table.Column<string>(type: "text", nullable: true),
                    NewEntityType = table.Column<string>(type: "text", nullable: true),
                    OriginalContent = table.Column<string>(type: "text", nullable: true),
                    ProposedContent = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteProposals_ConversationMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ConversationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NoteProposals_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteProposals_ConversationId",
                table: "NoteProposals",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteProposals_MessageId",
                table: "NoteProposals",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteProposals_Status",
                table: "NoteProposals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteProposals");
        }
    }
}
