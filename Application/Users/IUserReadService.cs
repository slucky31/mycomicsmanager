using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Users;

namespace Application.Users;

public interface IUserReadService
{
    Task<IPagedList<User>> GetUsersAsync(string? searchTerm, UsersColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize);

    Task<Result<User>> GetUserByEmail(string? email);

    Task<Result<User>> GetUserByAuthIdOrEmail(string? email, string? authId);

}
