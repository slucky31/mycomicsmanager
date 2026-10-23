using Application.Series;

namespace Application.UnitTests.Series;

internal static class SerieRows
{
    public static SerieBookRow Book(Guid libraryId, string serie, int volume, string imageLink = "", string libraryName = "BD") =>
        new(Guid.CreateVersion7(), libraryId, libraryName, serie, $"{serie} {volume}", volume, imageLink);
}
