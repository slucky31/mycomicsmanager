using FluentValidation.Results;

namespace Web.Models;

// The errors of a form once it was submitted: one message under each field, and their count above the form.
public sealed class FormErrors
{
    public static readonly FormErrors None = new(new Dictionary<string, string>(StringComparer.Ordinal));

    private readonly Dictionary<string, string> _messages;

    private FormErrors(Dictionary<string, string> messages) => _messages = messages;

    public int Count => _messages.Count;

    public bool Any => _messages.Count > 0;

    public string Summary => Count == 1 ? "1 field needs your attention." : $"{Count} fields need your attention.";

    // The first error of each field, in the order of the rules.
    public static FormErrors From(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var messages = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var failure in result.Errors)
        {
            messages.TryAdd(failure.PropertyName, failure.ErrorMessage);
        }

        return new FormErrors(messages);
    }

    public string? For(string propertyName) => _messages.GetValueOrDefault(propertyName);

    // "true" on an invalid field (aria-invalid), nothing otherwise.
    public string? AriaInvalid(string propertyName) => _messages.ContainsKey(propertyName) ? "true" : null;
}
