using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class HeapingAlgoInt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HeapingAlgorithm",
            table: "Decks");

        migrationBuilder.AddColumn<int>(
            name: "HeapingAlgorithmId",
            table: "Decks",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HeapingAlgorithmId",
            table: "Decks");

        migrationBuilder.AddColumn<string>(
            name: "HeapingAlgorithm",
            table: "Decks",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }
}
