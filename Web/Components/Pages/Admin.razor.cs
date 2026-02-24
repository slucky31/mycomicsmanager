using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Components.Pages;

public partial class Admin
{
    private string _username = "Anonymous User";
    private string _picture = "";

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState is not null)
        {
            var state = await AuthenticationState;

            _username = state?.User?.Identity?.Name ?? _username;

            _picture = state?.User?.Claims?
                        .Where(c => c.Type.Equals("picture", StringComparison.Ordinal))
                        .Select(c => c.Value)
                        .FirstOrDefault() ?? string.Empty;
        }
        await base.OnInitializedAsync();
    }
}
