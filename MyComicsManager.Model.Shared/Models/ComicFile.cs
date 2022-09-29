namespace MyComicsManager.Model.Shared.Models;

public class ComicFile
{
    public string Name { get; init; }

    public long Size { get; init; }

    public string LibId { get; init; }
            
    public string Path { get; init; }

    public double UploadDuration { get; init; }

    public string ExceptionMessage { get; init; }
}