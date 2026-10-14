namespace Web.Models;

// Links a field to the texts under it (its error, then its help), for screen readers.
public static class FormFieldIds
{
    public static string ErrorOf(string id) => $"{id}-error";

    public static string HelpOf(string id) => $"{id}-help";

    // The value of aria-describedby, nothing when the field has neither an error nor a help.
    public static string? DescribedBy(string id, string? error, string? help)
    {
        var ids = new List<string>(2);
        if (!string.IsNullOrEmpty(error))
        {
            ids.Add(ErrorOf(id));
        }

        if (!string.IsNullOrEmpty(help))
        {
            ids.Add(HelpOf(id));
        }

        return ids.Count == 0 ? null : string.Join(' ', ids);
    }
}
