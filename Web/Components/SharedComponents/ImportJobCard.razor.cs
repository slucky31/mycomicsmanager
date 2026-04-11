using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.SharedComponents;

public partial class ImportJobCard
{
    [Parameter, EditorRequired]
    public ImportJobViewModel Job { get; set; } = default!;
}
