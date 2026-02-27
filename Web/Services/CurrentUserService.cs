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
        var email = authState.User.Identity?.Name;

        if (string.IsNullOrEmpty(email))
            return UsersError.NotFound;

        var userResult = await userReadService.GetUserByEmail(email);
        if (userResult.IsFailure)
            return userResult.Error;

        return userResult.Value!.Id;
    }
}
