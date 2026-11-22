using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveVirtualizedLibraryToggle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The prototype of #1344 became the only way to show a library: its toggle no longer exists, and a saved
        // override would not be read back into FeatureToggle.
        migrationBuilder.Sql("""DELETE FROM "FeatureToggleOverrides" WHERE "Toggle" = 'VirtualizedLibrary';""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Nothing to restore: the toggle is gone.
    }
}
