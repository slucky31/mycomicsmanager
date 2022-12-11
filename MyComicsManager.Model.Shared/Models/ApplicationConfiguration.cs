namespace MyComicsManager.Model.Shared.Models;

public static class ApplicationConfiguration
{
    public static readonly string CoversPath = "covers";
    public static readonly string IsbnPath = CoversPath + Path.DirectorySeparatorChar + "isbn";
    public static readonly string ThumbsPath = CoversPath + Path.DirectorySeparatorChar + "thumbs";

    public static readonly string ImportPath = "import";
    public static readonly string ImportErrorsPath = ImportPath + Path.DirectorySeparatorChar + "errors";

    public static readonly string LibsPath = "libs";

    public static readonly string[] AuthorizedExtensions = { "*.cbr", "*.cbz", "*.pdf", "*.zip", "*.rar" };

    public static readonly string ONE_SHOT_SERIE = "One shot";
}