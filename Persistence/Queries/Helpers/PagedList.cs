using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries.Helpers;

public class PagedList<T> : IPagedList<T>
{
    public PagedList(IQueryable<T> query)
    {
        _query = query;
        Page = -1;
        PageSize = -1;
        TotalCount = -1;
        Items = null;
    }

    private readonly IQueryable<T> _query;

    public IReadOnlyCollection<T>? Items { get; private set; }

    public int Page { get; private set; }

    public int PageSize { get; private set; }

    public int TotalCount { get; private set; }

    public bool HasNextPage => TotalCount != -1 && Page * PageSize < TotalCount;

    public bool HasPreviousPage => TotalCount != -1 && Page > 1;

    public async Task<PagedList<T>> ExecuteQueryAsync(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = await _query.CountAsync();
        Items = await _query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return this;
    }

}
