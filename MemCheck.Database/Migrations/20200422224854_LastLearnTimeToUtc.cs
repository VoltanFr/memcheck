using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class LastLearnTimeToUtc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "LastLearnTime",
table: "CardsInDecks",
newName: "LastLearnUtcTime");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "LastLearnUtcTime",
table: "CardsInDecks",
newName: "LastLearnTime");
    }
}
