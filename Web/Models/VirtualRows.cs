using Domain.Primitives;

namespace Web.Models;

// A row of a virtualized grid (#1344, #1366): as many items as the grid has columns, from the item at FirstItem.
public sealed record VirtualRow<TItem>(int FirstItem, IReadOnlyList<TItem> Items);

// The rows asked for, and how many rows the grid has.
public sealed record VirtualRowsPage<TItem>(IReadOnlyList<VirtualRow<TItem>> Rows, int RowCount);

// The items of a virtualized grid, cut into rows of as many items as the grid has columns.
public interface IVirtualRowSource<TItem>
{
    Task<Result<VirtualRowsPage<TItem>>> GetRowsAsync(int startRow, int rowCount, int columns, CancellationToken cancellationToken);

    // Whether these are other items than those of the source shown before (another sort, search or filter),
    // shown from the top; the same items again (one left meanwhile) stay where they are.
    bool ShowsOtherItemsThan(IVirtualRowSource<TItem>? other);
}

// The same row when it starts at the same place with the same item (Virtualize keeps it on screen when rows arrive).
public sealed class VirtualRowComparer<TItem>(Func<TItem, object> keyOf) : IEqualityComparer<VirtualRow<TItem>>
{
    public bool Equals(VirtualRow<TItem>? x, VirtualRow<TItem>? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && x.FirstItem == y.FirstItem && Equals(FirstKey(x), FirstKey(y)));

    public int GetHashCode(VirtualRow<TItem> obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return HashCode.Combine(obj.FirstItem, FirstKey(obj));
    }

    private object? FirstKey(VirtualRow<TItem> row) => row.Items.Count > 0 ? keyOf(row.Items[0]) : null;
}

public static class VirtualRows
{
    public static int CountOf(int itemCount, int columns) => (itemCount + columns - 1) / columns;

    public static void CheckRequest(int startRow, int rowCount, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startRow);
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
    }
}

// Items already loaded, such as the series of a library or the volumes of a series (#1366), cut into rows.
// Scope tells which items they are: another scope (another filter, another series) is shown from the top.
public sealed class ListRows<TItem>(IReadOnlyList<TItem> items, string scope) : IVirtualRowSource<TItem>
{
    public IReadOnlyList<TItem> Items { get; } = items;
    public string Scope { get; } = scope;

    public Task<Result<VirtualRowsPage<TItem>>> GetRowsAsync(int startRow, int rowCount, int columns, CancellationToken cancellationToken)
    {
        VirtualRows.CheckRequest(startRow, rowCount, columns);

        var rows = new List<VirtualRow<TItem>>(rowCount);
        var end = Math.Min((startRow + rowCount) * columns, Items.Count);
        for (var first = startRow * columns; first < end; first += columns)
        {
            rows.Add(new VirtualRow<TItem>(first, [.. Items.Skip(first).Take(Math.Min(columns, end - first))]));
        }

        Result<VirtualRowsPage<TItem>> page = new VirtualRowsPage<TItem>(rows, VirtualRows.CountOf(Items.Count, columns));
        return Task.FromResult(page);
    }

    public bool ShowsOtherItemsThan(IVirtualRowSource<TItem>? other) =>
        other is not ListRows<TItem> list || !string.Equals(list.Scope, Scope, StringComparison.Ordinal);
}
