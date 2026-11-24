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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0013", Justification = "The state is read again on every call, so it is never stale.")]
    public async Task<Result<Guid>> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var userId = await UserIdentity.ResolveUserIdAsync(authState.User, userReadService, cancellationToken);
        return userId is null ? UsersError.NotFound : userId.Value;
    }
}
