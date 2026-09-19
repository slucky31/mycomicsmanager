using Microsoft.AspNetCore.Components;
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
}
