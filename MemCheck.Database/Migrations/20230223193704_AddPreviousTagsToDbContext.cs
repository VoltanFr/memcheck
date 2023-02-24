using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

/// <inheritdoc />
public partial class AddPreviousTagsToDbContext : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TagPreviousVersion_AspNetUsers_CreatingUserId",
            table: "TagPreviousVersion");

        migrationBuilder.DropForeignKey(
            name: "FK_TagPreviousVersion_TagPreviousVersion_PreviousVersionId",
            table: "TagPreviousVersion");

        migrationBuilder.DropForeignKey(
            name: "FK_Tags_TagPreviousVersion_PreviousVersionId",
            table: "Tags");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TagPreviousVersion",
            table: "TagPreviousVersion");

        migrationBuilder.RenameTable(
            name: "TagPreviousVersion",
            newName: "TagPreviousVersions");

        migrationBuilder.RenameIndex(
            name: "IX_TagPreviousVersion_PreviousVersionId",
            table: "TagPreviousVersions",
            newName: "IX_TagPreviousVersions_PreviousVersionId");

        migrationBuilder.RenameIndex(
            name: "IX_TagPreviousVersion_CreatingUserId",
            table: "TagPreviousVersions",
            newName: "IX_TagPreviousVersions_CreatingUserId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TagPreviousVersions",
            table: "TagPreviousVersions",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_TagPreviousVersions_AspNetUsers_CreatingUserId",
            table: "TagPreviousVersions",
            column: "CreatingUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_TagPreviousVersions_TagPreviousVersions_PreviousVersionId",
            table: "TagPreviousVersions",
            column: "PreviousVersionId",
            principalTable: "TagPreviousVersions",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Tags_TagPreviousVersions_PreviousVersionId",
            table: "Tags",
            column: "PreviousVersionId",
            principalTable: "TagPreviousVersions",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TagPreviousVersions_AspNetUsers_CreatingUserId",
            table: "TagPreviousVersions");

        migrationBuilder.DropForeignKey(
            name: "FK_TagPreviousVersions_TagPreviousVersions_PreviousVersionId",
            table: "TagPreviousVersions");

        migrationBuilder.DropForeignKey(
            name: "FK_Tags_TagPreviousVersions_PreviousVersionId",
            table: "Tags");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TagPreviousVersions",
            table: "TagPreviousVersions");

        migrationBuilder.RenameTable(
            name: "TagPreviousVersions",
            newName: "TagPreviousVersion");

        migrationBuilder.RenameIndex(
            name: "IX_TagPreviousVersions_PreviousVersionId",
            table: "TagPreviousVersion",
            newName: "IX_TagPreviousVersion_PreviousVersionId");

        migrationBuilder.RenameIndex(
            name: "IX_TagPreviousVersions_CreatingUserId",
            table: "TagPreviousVersion",
            newName: "IX_TagPreviousVersion_CreatingUserId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TagPreviousVersion",
            table: "TagPreviousVersion",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_TagPreviousVersion_AspNetUsers_CreatingUserId",
            table: "TagPreviousVersion",
            column: "CreatingUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_TagPreviousVersion_TagPreviousVersion_PreviousVersionId",
            table: "TagPreviousVersion",
            column: "PreviousVersionId",
            principalTable: "TagPreviousVersion",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Tags_TagPreviousVersion_PreviousVersionId",
            table: "Tags",
            column: "PreviousVersionId",
            principalTable: "TagPreviousVersion",
            principalColumn: "Id");
    }
}
