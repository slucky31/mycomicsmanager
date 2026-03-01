using System.Security.Claims;
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
        var principal = authState.User;

        // Try stable sub claim first (raw "sub" or .NET OIDC-mapped to NameIdentifier)
        var sub = principal.FindFirstValue("sub")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(sub))
        {
            var byAuthId = await userReadService.GetUserByAuthId(sub);
            if (byAuthId.IsSuccess)
            {
                return byAuthId.Value!.Id;
            }
        }

        // Fallback: email lookup for users not yet migrated to sub-based AuthId
        var email = principal.Identity?.Name;
        if (string.IsNullOrEmpty(email))
        {
            return UsersError.NotFound;
        }

        var byEmail = await userReadService.GetUserByEmail(email);
        if (byEmail.IsFailure)
        {
            return byEmail.Error!;
        }

        return byEmail.Value!.Id;
    }
}
