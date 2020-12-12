using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class RenameExcludeAllTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "excludeAllTags",
                table: "SearchSubscriptions",
                newName: "ExcludeAllTags");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExcludeAllTags",
                table: "SearchSubscriptions",
                newName: "excludeAllTags");
        }
    }
}
