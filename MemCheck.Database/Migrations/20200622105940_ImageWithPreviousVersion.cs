using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class ImageWithPreviousVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImagePreviousVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ImageId = table.Column<Guid>(nullable: false),
                    OwnerId = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    Source = table.Column<string>(nullable: false),
                    InitialUploadUtcDate = table.Column<DateTime>(nullable: false),
                    VersionUtcDate = table.Column<DateTime>(nullable: false),
                    OriginalContentType = table.Column<string>(nullable: false),
                    OriginalSize = table.Column<int>(nullable: false),
                    OriginalBlob = table.Column<byte[]>(nullable: false),
                    VersionType = table.Column<int>(type: "int", nullable: false),
                    VersionDescription = table.Column<string>(nullable: false),
                    PreviousVersionId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagePreviousVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagePreviousVersion_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImagePreviousVersion_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImagePreviousVersion_ImagePreviousVersion_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "ImagePreviousVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagePreviousVersion_ImageId",
                table: "ImagePreviousVersion",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagePreviousVersion_OwnerId",
                table: "ImagePreviousVersion",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagePreviousVersion_PreviousVersionId",
                table: "ImagePreviousVersion",
                column: "PreviousVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagePreviousVersion");
        }
    }
}
