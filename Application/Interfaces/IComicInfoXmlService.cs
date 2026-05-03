using Domain.Primitives;

namespace Application.Interfaces;

public record ComicInfoData(
    string? Title,
    string? Series,
    int? Number,
    string? Summary,
    int? Year,
    int? Month,
    int? Day,
    string? Writer,
    string? Penciller,
    string? Publisher,
    string? Isbn,
    int? PageCount);

public interface IComicInfoXmlService
{
    /// <summary>
    /// Reads a ComicInfo.xml file and returns the parsed metadata.
    /// </summary>
    Result<ComicInfoData> Read(string xmlPath);

    /// <summary>
    /// Writes metadata to a ComicInfo.xml file, omitting null fields.
    /// </summary>
    Result Write(string xmlPath, ComicInfoData data);
}
