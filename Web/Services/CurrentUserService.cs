using Application.Interfaces;
using Application.Users;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Services;

public class CurrentUserService(
    AuthenticationStateProvider authStateProvider,
    IUserReadService userReadService) : ICurrentUserService
{
    public async Task<Result<Guid>> GetCurrentUserIdAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var sub = authState.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(sub))
        {
            return UsersError.NotFound;
        }

        var userResult = await userReadService.GetUserByAuthId(sub);
        if (userResult.IsFailure)
        {
            return userResult.Error!;
        }

        return userResult.Value!.Id;
    }
}
