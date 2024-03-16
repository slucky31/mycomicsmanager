using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace Web;

public class CustomAuthenticationStateProvider : ServerAuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authState = await base.GetAuthenticationStateAsync();
        var user = authState.User;
             
        // return the modified principal
        return await Task.FromResult(new AuthenticationState(user));
    }
}
