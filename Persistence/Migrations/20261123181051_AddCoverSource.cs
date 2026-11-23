using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddCoverSource : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CoverSource",
            table: "DigitalBooks",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        // Until #1225, the first pages of the archives were uploaded to digital-covers (CoverSource.Archive = 1).
        migrationBuilder.Sql("UPDATE \"DigitalBooks\" d SET \"CoverSource\" = 1 FROM \"Books\" b WHERE b.\"Id\" = d.\"Id\" AND b.\"ImageLink\" LIKE '%/digital-covers/%';");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CoverSource",
            table: "DigitalBooks");
    }
}
