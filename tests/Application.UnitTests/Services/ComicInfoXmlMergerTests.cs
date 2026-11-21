using System.Xml.Linq;
using Application.Interfaces;
using Persistence.Services;

namespace Application.UnitTests.Services;

public class ComicInfoXmlMergerTests
{
    private static readonly ComicInfoData s_data = new(
        Title: "Âme rouge", Series: "Blacksad", Number: 3, Summary: null, Year: 2005, Month: 11, Day: 4,
        Writer: "Juan Díaz Canales", Penciller: null, Publisher: "Dargaud", Isbn: null, PageCount: null);

    [Fact]
    public void Merge_Should_WriteTheElementsInTheNamespaceOfTheFile_WithoutDuplicates()
    {
        XNamespace ns = "http://example.org/comicinfo";
        var existing = new XDocument(new XElement(ns + "ComicInfo",
            new XElement(ns + "Title", "Old title"),
            new XElement(ns + "ISBN", "9782205050196"),
            new XElement(ns + "Summary", "Un chat noir...")));

        var root = ComicInfoXmlMerger.Merge(existing, s_data).Root!;

        root.Elements().Should().OnlyContain(e => e.Name.Namespace == ns);
        root.Elements(ns + "Title").Should().ContainSingle().Which.Value.Should().Be("Âme rouge");
        root.Element(ns + "Month")!.Value.Should().Be("11");
        root.Element(ns + "Day")!.Value.Should().Be("4");
        root.Element(ns + "ISBN").Should().BeNull();
        root.Element(ns + "Summary")!.Value.Should().Be("Un chat noir...");
    }

    [Fact]
    public void Merge_Should_LeaveTheGivenDocumentUntouched()
    {
        var existing = XDocument.Parse("<ComicInfo><Title>Old title</Title></ComicInfo>");

        _ = ComicInfoXmlMerger.Merge(existing, s_data);

        existing.Root!.Elements().Should().ContainSingle().Which.Value.Should().Be("Old title");
    }

    [Fact]
    public void Merge_Should_Throw_WhenTheDataIsNull()
    {
        var act = () => ComicInfoXmlMerger.Merge(null, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
