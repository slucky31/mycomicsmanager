using System.Globalization;
using System.Xml.Linq;
using Application.Interfaces;

namespace Persistence.Services;

/// <summary>
/// Puts the metadata of a book into a ComicInfo.xml document, keeping the elements the book does not own.
/// </summary>
public static class ComicInfoXmlMerger
{
    public static XDocument Merge(XDocument? existing, ComicInfoData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var document = existing?.Root?.Name.LocalName == "ComicInfo"
            ? new XDocument(existing)
            : new XDocument(new XElement("ComicInfo"));
        var root = document.Root!;

        // Owned by the book: an empty value removes the element.
        SetOrRemove(root, "Title", data.Title);
        SetOrRemove(root, "Series", data.Series);
        SetOrRemove(root, "Number", Format(data.Number));
        SetOrRemove(root, "Volume", Format(data.Number));
        SetOrRemove(root, "Year", Format(data.Year));
        SetOrRemove(root, "Month", Format(data.Month));
        SetOrRemove(root, "Day", Format(data.Day));
        SetOrRemove(root, "Writer", data.Writer);
        SetOrRemove(root, "Publisher", data.Publisher);
        SetOrRemove(root, "ISBN", data.Isbn);

        // Not always known by the book: an empty value keeps what the file says.
        SetIfAny(root, "Summary", data.Summary);
        SetIfAny(root, "Penciller", data.Penciller);
        SetIfAny(root, "PageCount", Format(data.PageCount));

        return document;
    }

    private static void SetOrRemove(XElement root, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            root.Elements(root.Name.Namespace + name).Remove();
            return;
        }

        SetIfAny(root, name, value);
    }

    private static void SetIfAny(XElement root, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var element = root.Element(root.Name.Namespace + name);
        if (element is null)
        {
            root.Add(new XElement(root.Name.Namespace + name, value));
        }
        else
        {
            element.Value = value;
        }
    }

    private static string? Format(int? value) => value?.ToString(CultureInfo.InvariantCulture);
}
