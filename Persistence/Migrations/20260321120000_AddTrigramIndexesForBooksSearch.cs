using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddTrigramIndexesForBooksSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        migrationBuilder.Sql(
            "CREATE INDEX IF NOT EXISTS \"IX_Books_Title_trgm\" " +
            "ON \"Books\" USING GIN (\"Title\" gin_trgm_ops);");

        migrationBuilder.Sql(
            "CREATE INDEX IF NOT EXISTS \"IX_Books_Serie_trgm\" " +
            "ON \"Books\" USING GIN (\"Serie\" gin_trgm_ops);");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Books_Title_trgm\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Books_Serie_trgm\";");
    }
}
