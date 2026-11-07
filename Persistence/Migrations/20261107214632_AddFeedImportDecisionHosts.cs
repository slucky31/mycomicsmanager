using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedImportDecisionHosts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string[]>(
            name: "Hosts",
            table: "FeedImportDecisions",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());

        // The decisions already analysed get the hosts of the links they keep (every mirror of every candidate).
        migrationBuilder.Sql("""
            UPDATE "FeedImportDecisions" AS d
            SET "Hosts" = COALESCE((
                SELECT array_agg(DISTINCT lower(btrim(m ->> 'host')) ORDER BY lower(btrim(m ->> 'host')))
                FROM jsonb_array_elements(d."Links") AS c,
                     jsonb_array_elements(CASE WHEN jsonb_typeof(c -> 'mirrors') = 'array' THEN c -> 'mirrors' ELSE '[]'::jsonb END) AS m
                WHERE COALESCE(btrim(m ->> 'host'), '') <> ''), '{}')
            WHERE d."Links" IS NOT NULL AND jsonb_typeof(d."Links") = 'array';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Hosts",
            table: "FeedImportDecisions");
    }
}
