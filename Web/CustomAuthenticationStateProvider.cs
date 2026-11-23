using System.Security.Claims;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Web.Services;

namespace Web;

internal sealed class CustomAuthenticationStateProvider(
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0013", Justification = "This is the provider itself: base.GetAuthenticationStateAsync reads its own current state.")]
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authState = await base.GetAuthenticationStateAsync();
        var user = authState.User;

        Guard.Against.Null(user.Identity);
        if (user.Identity.IsAuthenticated)
        {
            var sub = UserIdentity.GetSub(user);
            if (!string.IsNullOrEmpty(sub))
            {
                // A failed provisioning is retried on the next call.
                if (_provisioning is null || _provisionedSub != sub || _provisioning.IsFaulted || _provisioning.IsCanceled)
                {
                    _provisionedSub = sub;
                    _provisioning = ProvisionAsync(sub, user);
                }
                await _provisioning;
            }
        }

        return authState;
    }

    // An unknown sub gets its user: the legacy user with its verified e-mail is linked to it (once),
    // otherwise a new user is created, unless the e-mail already belongs to a user linked to another sub.
    private async Task ProvisionAsync(string sub, ClaimsPrincipal principal)
    {
        var userByAuthId = await _userReadService.GetUserByAuthId(sub);
        if (userByAuthId.IsSuccess || userByAuthId.Error != UsersError.NotFound)
        {
            return;
        }

        var verifiedEmail = UserIdentity.GetVerifiedEmail(principal);
        var email = verifiedEmail ?? principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        if (string.IsNullOrEmpty(email))
        {
            return;
        }

        var userByEmail = await _userReadService.GetUserByEmail(email);
        if (userByEmail.IsSuccess)
        {
            await LinkLegacyUserAsync(userByEmail.Value!, sub, verifiedEmail);
            return;
        }

        _userRepository.Add(User.Create(email, sub));
        await _unitOfWork.SaveChangesAsync(default);
    }

    private async Task LinkLegacyUserAsync(User existing, string sub, string? verifiedEmail)
    {
        if (verifiedEmail is null || !UserIdentity.IsLegacy(existing))
        {
            // The e-mail is not proven, or its user belongs to another Auth0 account.
            return;
        }

        var tracked = await _userRepository.GetByIdAsync(existing.Id);
        if (tracked is null)
        {
            return;
        }

        tracked.Update(tracked.Email, sub);
        await _unitOfWork.SaveChangesAsync(default);
    }
}
