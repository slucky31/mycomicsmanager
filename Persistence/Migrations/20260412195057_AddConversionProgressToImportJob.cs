using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddConversionProgressToImportJob : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ConvertedImagesCount",
            table: "ImportJobs",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "TotalImagesToConvert",
            table: "ImportJobs",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConvertedImagesCount",
            table: "ImportJobs");

        migrationBuilder.DropColumn(
            name: "TotalImagesToConvert",
            table: "ImportJobs");
    }
}
