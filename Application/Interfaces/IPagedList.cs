namespace Application.Interfaces;

public interface IPagedList<T>
{
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
    IReadOnlyCollection<T>? Items { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }

    Task<IPagedList<T>> ExecuteQueryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
