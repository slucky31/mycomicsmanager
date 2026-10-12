using Domain.Books;

namespace Web.Models;

// The rows of the reader: one page, or two side by side in the "Two pages" layout.
// Each row lists its pages in screen order (right to left for mangas).
public sealed class ReaderSpreads
{
    private readonly IReadOnlyList<IReadOnlyList<int>> _rows;
    private readonly int[] _rowOfPage;

    private ReaderSpreads(IReadOnlyList<IReadOnlyList<int>> rows, int pageCount)
    {
        _rows = rows;
        _rowOfPage = new int[pageCount];
        for (var row = 0; row < rows.Count; row++)
        {
            foreach (var page in rows[row])
            {
                _rowOfPage[page] = row;
            }
        }
    }

    public int Count => _rows.Count;

    public IReadOnlyList<int> PagesOf(int row) => _rows[row];

    public int FirstPageOf(int row) => _rows[row].Min();

    public int LastPageOf(int row) => _rows[row].Max();

    public int RowOf(int pageIndex) => _rowOfPage[Math.Clamp(pageIndex, 0, _rowOfPage.Length - 1)];

    // "5 / 188", or "4-5 / 188" for two pages side by side.
    public string Counter(int row)
    {
        var first = FirstPageOf(row) + 1;
        var last = LastPageOf(row) + 1;
        var pages = first == last ? $"{first}" : $"{first}-{last}";
        return $"{pages} / {_rowOfPage.Length}";
    }

    public static ReaderSpreads Build(int pageCount, ReaderSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);
        ArgumentNullException.ThrowIfNull(settings);

        var rows = settings.Layout == ReaderLayout.TwoPages
            ? BuildPairs(pageCount, settings.Direction == ReadingDirection.RightToLeft)
            : Enumerable.Range(0, pageCount).Select(page => (IReadOnlyList<int>)[page]).ToList();
        return new ReaderSpreads(rows, pageCount);
    }

    // The cover stands alone, as in a printed book: then the pages go by pairs.
    private static List<IReadOnlyList<int>> BuildPairs(int pageCount, bool rightToLeft)
    {
        List<IReadOnlyList<int>> rows = [[0]];
        for (var first = 1; first < pageCount; first += 2)
        {
            if (first + 1 >= pageCount)
            {
                rows.Add([first]);
            }
            else
            {
                rows.Add(rightToLeft ? [first + 1, first] : [first, first + 1]);
            }
        }

        return rows;
    }
}
