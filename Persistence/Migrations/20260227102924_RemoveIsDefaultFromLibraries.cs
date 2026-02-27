using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsDefaultFromLibraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert any libraries with BookType = 2 (All, now removed) to BookType = 0 (Physical)
            migrationBuilder.Sql("UPDATE \"Libraries\" SET \"BookType\" = 0 WHERE \"BookType\" = 2;");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Libraries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Libraries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
