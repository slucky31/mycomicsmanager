namespace Application.ImportJobs;

public class ImportSettings
{
    public string ImportDirectory { get; set; } = "/data/import";
    public string TempDirectory { get; set; } = "/data/temp";
    public int ImageTargetWidth { get; set; } = 1400;
    public int PollingIntervalSeconds { get; set; } = 30;
    public IReadOnlyList<string> SupportedExtensions { get; set; } = [".cbz", ".cbr", ".zip", ".rar", ".pdf"];
    public int MaxFileSizeMb { get; set; } = 500;

    // Folder of the old library (top-level files only), moved batch by batch into the "Imported" library from the
    // Settings page. Empty: the migration is off.
    public string LegacyLibraryDirectory { get; set; } = string.Empty;
}
