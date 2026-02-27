using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.CreateDefault;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Web;

internal class CustomAuthenticationStateProvider(
    IUserReadService userReadService,
    IRepository<User, Guid> userRepository,
    IUnitOfWork unitOfWork,
    ICommandHandler<CreateDefaultLibraryCommand, Library> createDefaultLibraryHandler) : ServerAuthenticationStateProvider
{
    private readonly IUserReadService _userReadService = userReadService;
    private readonly IRepository<User, Guid> _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICommandHandler<CreateDefaultLibraryCommand, Library> _createDefaultLibraryHandler = createDefaultLibraryHandler;

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

                // Create their default "Read Books" library
                await _createDefaultLibraryHandler.Handle(
                    new CreateDefaultLibraryCommand(usr.Id),
                    CancellationToken.None);
            }
        }

        // return the modified principal
        return await Task.FromResult(new AuthenticationState(user));
    }
}
