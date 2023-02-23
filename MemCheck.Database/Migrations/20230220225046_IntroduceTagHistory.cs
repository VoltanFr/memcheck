using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

/// <inheritdoc />
public partial class IntroduceTagHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CreatingUserId",
            table: "Tags",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "PreviousVersionId",
            table: "Tags",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VersionDescription",
            table: "Tags",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "VersionType",
            table: "Tags",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "VersionUtcDate",
            table: "Tags",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.CreateTable(
            name: "TagPreviousVersion",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tag = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatingUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                VersionDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                VersionType = table.Column<int>(type: "int", nullable: false),
                PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TagPreviousVersion", x => x.Id);
                table.ForeignKey(
                    name: "FK_TagPreviousVersion_AspNetUsers_CreatingUserId",
                    column: x => x.CreatingUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TagPreviousVersion_TagPreviousVersion_PreviousVersionId",
                    column: x => x.PreviousVersionId,
                    principalTable: "TagPreviousVersion",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tags_CreatingUserId",
            table: "Tags",
            column: "CreatingUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Name",
            table: "Tags",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tags_PreviousVersionId",
            table: "Tags",
            column: "PreviousVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_TagPreviousVersion_CreatingUserId",
            table: "TagPreviousVersion",
            column: "CreatingUserId");

        migrationBuilder.CreateIndex(
            name: "IX_TagPreviousVersion_PreviousVersionId",
            table: "TagPreviousVersion",
            column: "PreviousVersionId");

        migrationBuilder.AddForeignKey(
            name: "FK_Tags_AspNetUsers_CreatingUserId",
            table: "Tags",
            column: "CreatingUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Tags_TagPreviousVersion_PreviousVersionId",
            table: "Tags",
            column: "PreviousVersionId",
            principalTable: "TagPreviousVersion",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Tags_AspNetUsers_CreatingUserId",
            table: "Tags");

        migrationBuilder.DropForeignKey(
            name: "FK_Tags_TagPreviousVersion_PreviousVersionId",
            table: "Tags");

        migrationBuilder.DropTable(
            name: "TagPreviousVersion");

        migrationBuilder.DropIndex(
            name: "IX_Tags_CreatingUserId",
            table: "Tags");

        migrationBuilder.DropIndex(
            name: "IX_Tags_Name",
            table: "Tags");

        migrationBuilder.DropIndex(
            name: "IX_Tags_PreviousVersionId",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "CreatingUserId",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "PreviousVersionId",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "VersionDescription",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "VersionType",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "VersionUtcDate",
            table: "Tags");
    }
}
