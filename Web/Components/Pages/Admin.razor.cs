using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Components.Pages;

public partial class Admin
{
    private string _username = "Anonymous User";
    private string _picture = "";

    [CascadingParameter]
    private Task<AuthenticationState>? authenticationState { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if (authenticationState is not null)
        {
            var state = await authenticationState;

            _username = state?.User?.Identity?.Name ?? string.Empty;

            _picture = state?.User?.Claims?
                        .Where(c => c.Type.Equals("picture", StringComparison.Ordinal))
                        .Select(c => c.Value)
                        .FirstOrDefault() ?? string.Empty;
        }
        await base.OnInitializedAsync();
    }
}
