using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class RenameCreationDateInCard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
    name: "CreationUtcDate",
    table: "Cards",
    newName: "InitialCreationUtcDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
    name: "InitialCreationUtcDate",
    table: "Cards",
    newName: "CreationUtcDate");
        }
    }
}