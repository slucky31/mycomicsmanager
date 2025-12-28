using System.Security.Claims;
using Application.Interfaces;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Web;

internal class CustomAuthenticationStateProvider(IUserReadService userReadService, IRepository<User, Guid> userRepository, IUnitOfWork unitOfWork) : ServerAuthenticationStateProvider
{
    private readonly IUserReadService _userReadService = userReadService;
    private readonly IRepository<User, Guid> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {

        var authState = await base.GetAuthenticationStateAsync();
        var user = authState.User;

        Guard.Against.Null(user.Identity);
        if (user.Identity.IsAuthenticated)
        {
            var email = user.Identity.Name;
            var authId = user.FindFirstValue("sid");

            var userResult = await _userReadService.GetUserByEmail(email);
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(authId) && userResult.IsFailure && userResult.Error == UsersError.NotFound)
            {
                var usr = User.Create(email, authId);
                _userRepository.Add(usr);
                await _unitOfWork.SaveChangesAsync(default);
            }
        }

        // return the modified principal
        return await Task.FromResult(new AuthenticationState(user));
    }
}
