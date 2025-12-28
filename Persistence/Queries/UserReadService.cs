using System.Linq.Expressions;
using Application.Interfaces;
using Application.Users;
using Domain.Primitives;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;
public class UserReadService(ApplicationDbContext context) : IUserReadService
{
    public async Task<IPagedList<User>> GetUsersAsync(string? searchTerm, UsersColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(l => l.Email.Contains(searchTerm));
        }

        Expression<Func<User, object>> keySelector = sortColumn switch
        {
            UsersColumn.Id => User => User.Id,
            UsersColumn.Email => User => User.Email,
            UsersColumn.AuthId => User => User.AuthId,
            _ => User => User.Id
        };

        query = sortOrder switch
        {
            SortOrder.Descending => query.OrderByDescending(keySelector),
            SortOrder.Ascending => query.OrderBy(keySelector),
            _ => query.OrderBy(keySelector)
        };
        var librariesPagedList = new PagedList<User>(query);
        return await librariesPagedList.ExecuteQueryAsync(page, pageSize, cancellationToken);
    }

    public async Task<Result<User>> GetUserByAuthIdAndEmail(string? email, string? authId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(authId))
        {
            return UsersError.BadRequest;
        }

        var query = context.Users.AsNoTracking();
        var user = await query.Where(u => u.Email == email && u.AuthId == authId).SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return UsersError.NotFound;
        }
        return user;
    }

    public async Task<Result<User>> GetUserByEmail(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return UsersError.BadRequest;
        }

        var query = context.Users.AsNoTracking();
        var user = await query.Where(u => u.Email == email).SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return UsersError.NotFound;
        }
        return user;
    }

}
