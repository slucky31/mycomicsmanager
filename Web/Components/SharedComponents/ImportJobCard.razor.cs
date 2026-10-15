using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;

namespace Web.Components.SharedComponents;

public partial class ImportJobCard
{
    [Parameter, EditorRequired]
    public ImportJobViewModel Job { get; set; } = default!;

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public EventCallback<Guid> OnForceFail { get; set; }

    private bool IsStuck => Job is { IsTerminal: false, Status: not "Pending" };

    private string StatusIcon => Job.StatusCssClass switch
    {
        "status-pending" => Icons.Material.Outlined.HourglassEmpty,
        "status-completed" => Icons.Material.Filled.Check,
        "status-failed" => Icons.Material.Filled.PriorityHigh,
        _ => Icons.Material.Filled.Sync
    };
}
