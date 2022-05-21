using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class RenameCardVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "LastChangeUtcDate",
table: "Cards",
newName: "VersionUtcDate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "VersionUtcDate",
table: "Cards",
newName: "LastChangeUtcDate");
    }
}
