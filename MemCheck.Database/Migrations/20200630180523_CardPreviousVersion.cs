using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class CardPreviousVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreviousVersionId",
                table: "Cards",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionDescription",
                table: "Cards",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VersionType",
                table: "Cards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CardPreviousVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Card = table.Column<Guid>(nullable: false),
                    VersionCreatorId = table.Column<Guid>(nullable: false),
                    CardLanguageId = table.Column<Guid>(nullable: false),
                    FrontSide = table.Column<string>(nullable: false),
                    BackSide = table.Column<string>(nullable: false),
                    AdditionalInfo = table.Column<string>(nullable: false),
                    VersionUtcDate = table.Column<DateTime>(nullable: false),
                    VersionType = table.Column<int>(type: "int", nullable: false),
                    VersionDescription = table.Column<string>(nullable: false),
                    PreviousVersionId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPreviousVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardPreviousVersions_CardLanguages_CardLanguageId",
                        column: x => x.CardLanguageId,
                        principalTable: "CardLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardPreviousVersions_CardPreviousVersions_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "CardPreviousVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardPreviousVersions_AspNetUsers_VersionCreatorId",
                        column: x => x.VersionCreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagesInCardPreviousVersions",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(nullable: false),
                    CardPreviousVersionId = table.Column<Guid>(nullable: false),
                    CardSide = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagesInCardPreviousVersions", x => new { x.ImageId, x.CardPreviousVersionId });
                    table.ForeignKey(
                        name: "FK_ImagesInCardPreviousVersions_CardPreviousVersions_CardPreviousVersionId",
                        column: x => x.CardPreviousVersionId,
                        principalTable: "CardPreviousVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagInPreviousCardVersions",
                columns: table => new
                {
                    TagId = table.Column<Guid>(nullable: false),
                    CardPreviousVersionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagInPreviousCardVersions", x => new { x.CardPreviousVersionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TagInPreviousCardVersions_CardPreviousVersions_CardPreviousVersionId",
                        column: x => x.CardPreviousVersionId,
                        principalTable: "CardPreviousVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagInPreviousCardVersions_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsersWithViewOnCardPreviousVersions",
                columns: table => new
                {
                    AllowedUserId = table.Column<Guid>(nullable: false),
                    CardPreviousVersionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersWithViewOnCardPreviousVersions", x => new { x.CardPreviousVersionId, x.AllowedUserId });
                    table.ForeignKey(
                        name: "FK_UsersWithViewOnCardPreviousVersions_CardPreviousVersions_CardPreviousVersionId",
                        column: x => x.CardPreviousVersionId,
                        principalTable: "CardPreviousVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_PreviousVersionId",
                table: "Cards",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPreviousVersions_CardLanguageId",
                table: "CardPreviousVersions",
                column: "CardLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPreviousVersions_PreviousVersionId",
                table: "CardPreviousVersions",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPreviousVersions_VersionCreatorId",
                table: "CardPreviousVersions",
                column: "VersionCreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesInCardPreviousVersions_CardPreviousVersionId",
                table: "ImagesInCardPreviousVersions",
                column: "CardPreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TagInPreviousCardVersions_TagId",
                table: "TagInPreviousCardVersions",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardPreviousVersions_PreviousVersionId",
                table: "Cards",
                column: "PreviousVersionId",
                principalTable: "CardPreviousVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardPreviousVersions_PreviousVersionId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "ImagesInCardPreviousVersions");

            migrationBuilder.DropTable(
                name: "TagInPreviousCardVersions");

            migrationBuilder.DropTable(
                name: "UsersWithViewOnCardPreviousVersions");

            migrationBuilder.DropTable(
                name: "CardPreviousVersions");

            migrationBuilder.DropIndex(
                name: "IX_Cards_PreviousVersionId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "VersionDescription",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "VersionType",
                table: "Cards");
        }
    }
}
