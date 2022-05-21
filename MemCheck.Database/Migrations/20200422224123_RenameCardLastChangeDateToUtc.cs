﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class RenameCardLastChangeDateToUtc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "LastChangeDate",
table: "Cards",
newName: "LastChangeUtcDate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
name: "LastChangeUtcDate",
table: "Cards",
newName: "LastChangeDate");
    }
}
