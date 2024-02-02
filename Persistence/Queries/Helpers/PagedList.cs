using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries.Helpers;

public class PagedList<T> : IPagedList<T>
{
    public PagedList(IQueryable<T> query)
    {
        Query = query;
        Page = -1;
        PageSize = -1;
        TotalCount = -1;
        Items = null;
    }

    private IQueryable<T> Query;

    public IReadOnlyCollection<T>? Items { get; private set; }

    public int Page { get; private set; }

    public int PageSize { get; private set; }

    public int TotalCount { get; private set; }

    public bool HasNextPage => TotalCount == -1 ? false : Page * PageSize < TotalCount;

    public bool HasPreviousPage => TotalCount == -1 ? false : Page > 1;

    public async Task ExecuteQueryAsync(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = await Query.CountAsync();
        Items = await Query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

}
