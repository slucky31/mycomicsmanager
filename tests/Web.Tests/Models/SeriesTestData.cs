using Application.Series;
using Domain.Series;

namespace Web.Tests.Models;

internal static class SeriesTestData
{
    public static readonly Guid LibraryId = Guid.Parse("0193f3a8-0000-7000-8000-000000000001");

    public static SerieBookRow Book(string serie, int volume, string imageLink = "") =>
        new(Guid.CreateVersion7(), LibraryId, "BD", serie, $"{serie} {volume}", volume, imageLink);

    public static SerieSummary Summary(string name, int[] volumes, int? total = null, bool isFinished = false, string cover = "")
    {
        var completeness = SerieNames.IsOneShot(name) ? SerieCompleteness.NotApplicable : SerieCompleteness.Of(volumes, total);
        return new SerieSummary(LibraryId, name, volumes.Length, cover, volumes.Min(), volumes.Max(), total, isFinished, completeness);
    }
}
