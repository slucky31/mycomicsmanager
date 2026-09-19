using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddImportJobConcurrencyAndUniqueFilePath : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // "xmin" is a PostgreSQL system column that already exists on every table;
        // it only needs to be mapped in the EF model (ApplicationDbContext), never created/dropped here.
        migrationBuilder.CreateIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs",
            column: "OriginalFilePath",
            unique: true,
            filter: "\"Status\" NOT IN (6, 7)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ImportJobs_OriginalFilePath",
            table: "ImportJobs");
    }
}
