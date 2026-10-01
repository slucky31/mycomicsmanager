using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedImportDecisions : Migration
{
    private static readonly string[] s_userIdMinifluxEntryIdColumns = ["UserId", "MinifluxEntryId"];
    private static readonly string[] s_userIdStatusColumns = ["UserId", "Status"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // "xmin" is a PostgreSQL system column that already exists on every table;
        // it is only mapped as a concurrency token in the EF model, never created here.
        migrationBuilder.CreateTable(
            name: "FeedImportDecisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                MinifluxEntryId = table.Column<long>(type: "bigint", nullable: false),
                EntryTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                EntryUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ParsedSerie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ParsedTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                ParsedVolume = table.Column<int>(type: "integer", nullable: true),
                Links = table.Column<string>(type: "jsonb", nullable: true),
                ChosenMirror = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                MatchedBookId = table.Column<Guid>(type: "uuid", nullable: true),
                ImportJobId = table.Column<Guid>(type: "uuid", nullable: true),
                DigitalBookId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                DecidedBy = table.Column<int>(type: "integer", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ErrorStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedImportDecisions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FeedImportDecisionEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FeedImportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                DecidedBy = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedImportDecisionEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_FeedImportDecisionEvents_FeedImportDecisions_FeedImportDeci~",
                    column: x => x.FeedImportDecisionId,
                    principalTable: "FeedImportDecisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeedImportDecisionEvents_FeedImportDecisionId",
            table: "FeedImportDecisionEvents",
            column: "FeedImportDecisionId");

        migrationBuilder.CreateIndex(
            name: "IX_FeedImportDecisions_UserId_MinifluxEntryId",
            table: "FeedImportDecisions",
            columns: s_userIdMinifluxEntryIdColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FeedImportDecisions_UserId_Status",
            table: "FeedImportDecisions",
            columns: s_userIdStatusColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FeedImportDecisionEvents");

        migrationBuilder.DropTable(
            name: "FeedImportDecisions");
    }
}
