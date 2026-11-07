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

    [Parameter]
    public EventCallback<Guid> OnRetry { get; set; }

    // Receives the job, whose file name is shown in the confirmation.
    [Parameter]
    public EventCallback<ImportJobViewModel> OnAbandon { get; set; }

    private bool IsStuck => Job is { IsTerminal: false, Status: not "Pending" };

    private bool HasActions =>
        (Job.IsSucceeded && OnDelete.HasDelegate)
        || (Job.IsFailed && (OnRetry.HasDelegate || OnAbandon.HasDelegate))
        || (IsStuck && OnForceFail.HasDelegate);

    private string StatusIcon => Job.StatusCssClass switch
    {
        "status-pending" => Icons.Material.Outlined.HourglassEmpty,
        "status-completed" => Icons.Material.Filled.Check,
        "status-split" => Icons.Material.Filled.CallSplit,
        "status-failed" => Icons.Material.Filled.PriorityHigh,
        _ => Icons.Material.Filled.Sync
    };
}
