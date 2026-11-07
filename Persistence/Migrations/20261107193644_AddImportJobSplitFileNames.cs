using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddImportJobSplitFileNames : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs");

        migrationBuilder.AddColumn<string[]>(
            name: "SplitFileNames",
            table: "ImportJobs",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());

        migrationBuilder.CreateIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs",
            column: "OriginalFilePath",
            unique: true,
            filter: "\"Status\" NOT IN (6, 7, 8)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs");

        migrationBuilder.DropColumn(
            name: "SplitFileNames",
            table: "ImportJobs");

        migrationBuilder.CreateIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs",
            column: "OriginalFilePath",
            unique: true,
            filter: "\"Status\" NOT IN (6, 7)");
    }
}
