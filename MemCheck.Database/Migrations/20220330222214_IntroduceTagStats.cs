using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations
{
    public partial class IntroduceTagStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRatingOfPublicCards",
                table: "Tags",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "CountOfPublicCards",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRatingOfPublicCards",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CountOfPublicCards",
                table: "Tags");
        }
    }
}
