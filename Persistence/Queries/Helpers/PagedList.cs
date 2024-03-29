﻿using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries.Helpers;

public class PagedList<T>(IQueryable<T> query) : IPagedList<T>
{
    public IReadOnlyCollection<T>? Items { get; private set; }

    public int Page { get; private set; } = -1;

    public int PageSize { get; private set; } = -1;

    public int TotalCount { get; private set; } = -1;

    public bool HasNextPage => TotalCount != -1 && Page * PageSize < TotalCount;

    public bool HasPreviousPage => TotalCount != -1 && Page > 1;

    public async Task<IPagedList<T>> ExecuteQueryAsync(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = await query.CountAsync();
        Items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return this;
    }

}
