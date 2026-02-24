using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages;

public class ErrorPage : ComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected string? RequestId { get; set; }
    protected bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
