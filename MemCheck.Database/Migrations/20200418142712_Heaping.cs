using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class Heaping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeapingAlgorithm",
                table: "Decks",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeapingAlgorithm",
                table: "Decks");
        }
    }
}
