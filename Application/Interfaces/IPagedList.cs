namespace Application.Interfaces;

public interface IPagedList<out T>
{
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
    IReadOnlyCollection<T>? Items { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
}
