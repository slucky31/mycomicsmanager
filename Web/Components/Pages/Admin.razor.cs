using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Components.Pages;

public partial class Admin
{
    private string Username = "Anonymous User";
    private string Picture = "";

    [CascadingParameter]
    private Task<AuthenticationState>? authenticationState { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if (authenticationState is not null)
        {
            var state = await authenticationState;

            Username = state?.User?.Identity?.Name ?? string.Empty;

            Picture = state?.User?.Claims?
                        .Where(c => c.Type.Equals("picture", StringComparison.Ordinal))
                        .Select(c => c.Value)
                        .FirstOrDefault() ?? string.Empty;
        }
        await base.OnInitializedAsync();
    }
}
