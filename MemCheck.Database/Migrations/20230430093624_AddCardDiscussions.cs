using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

/// <inheritdoc />
public partial class AddCardDiscussions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "LatestDiscussionEntryId",
            table: "Cards",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CardDiscussionEntryPreviousVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Card = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreationUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardDiscussionEntryPreviousVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CardDiscussionEntryPreviousVersions_AspNetUsers_CreatorId",
                    column: x => x.CreatorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CardDiscussionEntryPreviousVersions_CardDiscussionEntryPreviousVersions_PreviousVersionId",
                    column: x => x.PreviousVersionId,
                    principalTable: "CardDiscussionEntryPreviousVersions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CardDiscussionDeletedEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Card = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DeletetionAuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DeletetionUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardDiscussionDeletedEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_CardDiscussionDeletedEntries_AspNetUsers_DeletetionAuthorId",
                    column: x => x.DeletetionAuthorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CardDiscussionDeletedEntries_CardDiscussionEntryPreviousVersions_PreviousVersionId",
                    column: x => x.PreviousVersionId,
                    principalTable: "CardDiscussionEntryPreviousVersions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CardDiscussionEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Card = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreationUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PreviousEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardDiscussionEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_CardDiscussionEntries_AspNetUsers_CreatorId",
                    column: x => x.CreatorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CardDiscussionEntries_CardDiscussionEntries_PreviousEntryId",
                    column: x => x.PreviousEntryId,
                    principalTable: "CardDiscussionEntries",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CardDiscussionEntries_CardDiscussionEntryPreviousVersions_PreviousVersionId",
                    column: x => x.PreviousVersionId,
                    principalTable: "CardDiscussionEntryPreviousVersions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Cards_LatestDiscussionEntryId",
            table: "Cards",
            column: "LatestDiscussionEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionDeletedEntries_DeletetionAuthorId",
            table: "CardDiscussionDeletedEntries",
            column: "DeletetionAuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionDeletedEntries_PreviousVersionId",
            table: "CardDiscussionDeletedEntries",
            column: "PreviousVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionEntries_CreatorId",
            table: "CardDiscussionEntries",
            column: "CreatorId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionEntries_PreviousEntryId",
            table: "CardDiscussionEntries",
            column: "PreviousEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionEntries_PreviousVersionId",
            table: "CardDiscussionEntries",
            column: "PreviousVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionEntryPreviousVersions_CreatorId",
            table: "CardDiscussionEntryPreviousVersions",
            column: "CreatorId");

        migrationBuilder.CreateIndex(
            name: "IX_CardDiscussionEntryPreviousVersions_PreviousVersionId",
            table: "CardDiscussionEntryPreviousVersions",
            column: "PreviousVersionId");

        migrationBuilder.AddForeignKey(
            name: "FK_Cards_CardDiscussionEntries_LatestDiscussionEntryId",
            table: "Cards",
            column: "LatestDiscussionEntryId",
            principalTable: "CardDiscussionEntries",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cards_CardDiscussionEntries_LatestDiscussionEntryId",
            table: "Cards");

        migrationBuilder.DropTable(
            name: "CardDiscussionDeletedEntries");

        migrationBuilder.DropTable(
            name: "CardDiscussionEntries");

        migrationBuilder.DropTable(
            name: "CardDiscussionEntryPreviousVersions");

        migrationBuilder.DropIndex(
            name: "IX_Cards_LatestDiscussionEntryId",
            table: "Cards");

        migrationBuilder.DropColumn(
            name: "LatestDiscussionEntryId",
            table: "Cards");
    }
}
