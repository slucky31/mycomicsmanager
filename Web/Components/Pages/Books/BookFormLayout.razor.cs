using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Books;

public partial class BookFormLayout
{
    // The id of the form, for the buttons of the header to submit it.
    [Parameter, EditorRequired]
    public string FormId { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    [Parameter]
    public FormErrors Errors { get; set; } = FormErrors.None;

    [Parameter]
    public string? ImageLink { get; set; }

    // Under the cover: the library of the book.
    [Parameter]
    public RenderFragment? Side { get; set; }

    // The fields, beside the cover from 840px.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    // The actions kept at the bottom of the screen on a phone.
    [Parameter]
    public RenderFragment? Footer { get; set; }
}
