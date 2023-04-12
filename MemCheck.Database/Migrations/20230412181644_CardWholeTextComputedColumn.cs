using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

/// <inheritdoc />
public partial class CardWholeTextComputedColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WholeText",
            table: "Cards",
            type: "nvarchar(max)",
            nullable: false,
            computedColumnSql: "[FrontSide] + [BackSide] + [AdditionalInfo] + [References]",
            stored: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WholeText",
            table: "Cards");
    }
}
