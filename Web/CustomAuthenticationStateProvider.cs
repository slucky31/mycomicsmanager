using System.Security.Claims;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Web;

internal class CustomAuthenticationStateProvider(
    IUserReadService userReadService,
    IRepository<User, Guid> userRepository,
    IUnitOfWork unitOfWork) : ServerAuthenticationStateProvider
{
    private readonly IUserReadService _userReadService = userReadService;
    private readonly IRepository<User, Guid> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Provisioning runs once per circuit: GetAuthenticationStateAsync is called concurrently (AuthorizeRouteView,
    // CurrentUserService...) and every caller shares the circuit's DbContext, so concurrent callers await the same task.
    private string? _provisionedSub;
    private Task? _provisioning;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authState = await base.GetAuthenticationStateAsync();
        var user = authState.User;

        Guard.Against.Null(user.Identity);
        if (user.Identity.IsAuthenticated)
        {
            var email = user.FindFirstValue(ClaimTypes.Email)
                     ?? user.FindFirstValue("email");

            // Auth0 sub claim: raw "sub" or mapped by .NET OIDC to NameIdentifier
            var sub = user.FindFirstValue("sub")
                   ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(sub))
            {
                // A failed provisioning is retried on the next call.
                if (_provisioning is null || _provisionedSub != sub || _provisioning.IsFaulted || _provisioning.IsCanceled)
                {
                    _provisionedSub = sub;
                    _provisioning = ProvisionAsync(sub, email);
                }
                await _provisioning;
            }
        }

        return authState;
    }

    private async Task ProvisionAsync(string sub, string? email)
    {
        var userByAuthId = await _userReadService.GetUserByAuthId(sub);
        if (userByAuthId.IsFailure && userByAuthId.Error == UsersError.NotFound
            && !string.IsNullOrEmpty(email))
        {
            var newUser = User.Create(email, sub);
            _userRepository.Add(newUser);
            await _unitOfWork.SaveChangesAsync(default);
        }
    }
}
