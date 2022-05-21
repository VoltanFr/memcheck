using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class Imagemodel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Images",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OwnerId = table.Column<Guid>(nullable: true),
                Name = table.Column<string>(nullable: false),
                Description = table.Column<string>(nullable: false),
                Source = table.Column<string>(nullable: false),
                ContentType = table.Column<string>(nullable: false),
                Size = table.Column<int>(nullable: false),
                Format = table.Column<int>(nullable: false),
                Blob = table.Column<byte[]>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Images", x => x.Id);
                table.ForeignKey(
                    name: "FK_Images_AspNetUsers_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Images_OwnerId",
            table: "Images",
            column: "OwnerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Images");
    }
}
