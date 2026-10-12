namespace Web.Models;

// Pages whose image the continuous reader requests: the visible page and its neighbours.
// A requested page is never released, so its height (and the scroll position) stays stable.
public sealed class ReaderPageWindow
{
    public const int PagesBehind = 1;
    public const int PagesAhead = 3;

    private readonly bool[] _requested;

    public ReaderPageWindow(int pageCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageCount);
        _requested = new bool[pageCount];
    }

    public int PageCount => _requested.Length;

    public bool IsRequested(int pageIndex) =>
        pageIndex >= 0 && pageIndex < _requested.Length && _requested[pageIndex];

    public void RequestAround(int pageIndex)
    {
        var first = Math.Max(0, pageIndex - PagesBehind);
        var last = Math.Min(_requested.Length - 1, pageIndex + PagesAhead);
        for (var i = first; i <= last; i++)
        {
            _requested[i] = true;
        }
    }
}
