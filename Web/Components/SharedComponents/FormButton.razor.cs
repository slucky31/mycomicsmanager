using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Web.Components.SharedComponents;

public enum FormButtonVariant
{
    Primary,
    Outlined,
    Text
}

public partial class FormButton
{
    [Parameter]
    public FormButtonVariant Variant { get; set; } = FormButtonVariant.Primary;

    // A link to this address instead of a button.
    [Parameter]
    public string? Href { get; set; }

    // "button" or "submit".
    [Parameter]
    public string Type { get; set; } = "button";

    // The id of the form submitted by a button placed outside of it (in the header bar).
    [Parameter]
    public string? Form { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    internal static string VariantClass(FormButtonVariant variant) => variant switch
    {
        FormButtonVariant.Outlined => "form-button-outlined",
        FormButtonVariant.Text => "form-button-text",
        _ => "form-button-primary",
    };

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? $"form-button {VariantClass(Variant)}"
        : $"form-button {VariantClass(Variant)} {Class}";
}
