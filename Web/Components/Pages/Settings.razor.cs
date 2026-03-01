using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Components.Pages;

public partial class Settings
{
    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _picture = string.Empty;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState is not null)
        {
            var state = await AuthenticationState;
            var user = state?.User;

            _picture = user?.Claims
                .FirstOrDefault(c => c.Type.Equals("picture", StringComparison.Ordinal))?.Value
                ?? string.Empty;

            _username = user?.Claims
                .FirstOrDefault(c => c.Type.Equals("name", StringComparison.Ordinal))?.Value
                ?? string.Empty;

            _email = user?.FindFirstValue(ClaimTypes.Email)
                  ?? user?.FindFirstValue("email")
                  ?? string.Empty;
        }
        await base.OnInitializedAsync();
    }
}
