using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class ChangesInImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "VersionType",
            table: "Images",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VersionType",
            table: "Images");
    }
}
