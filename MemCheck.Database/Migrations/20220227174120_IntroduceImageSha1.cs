using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations
{
    public partial class IntroduceImageSha1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "OriginalBlobSha1",
                table: "Images",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalBlobSha1",
                table: "Images");
        }
    }
}
