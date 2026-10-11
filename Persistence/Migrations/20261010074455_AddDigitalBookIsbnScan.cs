using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddDigitalBookIsbnScan : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string[]>(
            name: "IsbnCandidates",
            table: "DigitalBooks",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());

        migrationBuilder.AddColumn<DateTime>(
            name: "IsbnScannedAt",
            table: "DigitalBooks",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsbnCandidates",
            table: "DigitalBooks");

        migrationBuilder.DropColumn(
            name: "IsbnScannedAt",
            table: "DigitalBooks");
    }
}
