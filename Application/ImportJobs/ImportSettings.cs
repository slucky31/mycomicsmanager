namespace Application.ImportJobs;

public class ImportSettings
{
    public string ImportDirectory { get; set; } = "/data/import";
    public string TempDirectory { get; set; } = "/data/temp";
    public int ImageTargetWidth { get; set; } = 1400;
    public int PollingIntervalSeconds { get; set; } = 30;
    public IReadOnlyList<string> SupportedExtensions { get; set; } = [".cbz", ".cbr", ".zip", ".rar", ".pdf"];
    public int MaxFileSizeMb { get; set; } = 500;
}
