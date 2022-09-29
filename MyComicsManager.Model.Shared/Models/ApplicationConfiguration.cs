namespace MyComicsManager.Model.Shared.Models;

public static class ApplicationConfiguration
{
    public const string CoversPath = "covers";
    public static readonly string IsbnPath = CoversPath + Path.DirectorySeparatorChar + "isbn";
    public static readonly string ThumbsPath = CoversPath + Path.DirectorySeparatorChar + "thumbs";

    public const string ImportPath = "import";
    public static readonly string ErrorsPath = ImportPath + Path.DirectorySeparatorChar + "errors";

    public const string LibsPath = "libs";

    public static readonly string[] AuthorizedExtensions = { "*.cbr", "*.cbz", "*.pdf", "*.zip", "*.rar" };
}