using Application.ComicInfoSearch;
using HtmlAgilityPack;

namespace Application.UnitTests.ComicInfoSearch;

public class HtmlDataParserTests
{
    private readonly HtmlDataParser _parser;

    public HtmlDataParserTests()
    {
        _parser = new HtmlDataParser();
    }

    [Fact]
    public void LoadDocument_Should_ReturnTrue_WhenValidUrlProvided()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");

        // Act
        var result = _parser.LoadDocument(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ExtractSingleNode_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractSingleNode("//div");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractSingleNode_Should_ReturnNode_WhenDocumentIsLoaded()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
        _parser.LoadDocument(url);

        // Act
        var result = _parser.ExtractSingleNode("//html");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExtractSingleNodeFromCssClass_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractSingleNodeFromCssClass("div", "test-class");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractSingleNodeFromCssClass_Should_ReturnNode_WhenClassExists()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
        _parser.LoadDocument(url);

        // Act
        var result = _parser.ExtractSingleNodeFromCssClass("ul", "liste-albums");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExtractTextValue_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractTextValue("//div");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractTextValue_Should_ReturnEmptyString_WhenNodeNotFound()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
        _parser.LoadDocument(url);

        // Act
        var result = _parser.ExtractTextValue("//nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTextValue_Static_Should_ReturnEmptyString_WhenNodeIsNull()
    {
        // Act
        var result = HtmlDataParser.ExtractTextValue(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTextValue_Static_Should_ReturnTrimmedText_WhenNodeHasText()
    {
        // Arrange
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>  Some Text  </div>");
        var node = doc.DocumentNode.SelectSingleNode("//div");

        // Act
        var result = HtmlDataParser.ExtractTextValue(node);

        // Assert
        result.Should().Be("Some Text");
    }

    [Fact]
    public void ExtractTextValueAndSplitOnSeparatorFromDocument_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractTextValueAndSplitOnSeparatorFromDocument("//div", ":", 0);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractTextValueAndSplitOnSeparatorFromNode_Should_ReturnSplitValue_WhenSeparatorExists()
    {
        // Arrange
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>Label: Value</div>");
        var node = doc.DocumentNode.SelectSingleNode("//div");

        // Act
        var label = HtmlDataParser.ExtractTextValueAndSplitOnSeparatorFromNode(node, ":", 0);
        var value = HtmlDataParser.ExtractTextValueAndSplitOnSeparatorFromNode(node, ":", 1);

        // Assert
        label.Should().Be("Label");
        value.Should().Be("Value");
    }

    [Fact]
    public void ExtractTextValueAndSplitOnSeparatorFromNode_Should_ReturnFullText_WhenSeparatorNotFound()
    {
        // Arrange
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>No separator here</div>");
        var node = doc.DocumentNode.SelectSingleNode("//div");

        // Act
        var result = HtmlDataParser.ExtractTextValueAndSplitOnSeparatorFromNode(node, ":", 0);

        // Assert
        result.Should().Be("No separator here");
    }

    [Fact]
    public void ExtractTextValueAndSplitOnSeparatorFromNode_Should_ReturnEmptyString_WhenNodeIsNull()
    {
        // Act
        var result = HtmlDataParser.ExtractTextValueAndSplitOnSeparatorFromNode(null!, ":", 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractAttributValue_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractAttributValue("//a", "href");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractAttributValue_Should_ReturnEmptyString_WhenNodeNotFound()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
        _parser.LoadDocument(url);

        // Act
        var result = _parser.ExtractAttributValue("//nonexistent", "href");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractAttributValue_Should_ReturnAttributeValue_WhenNodeAndAttributeExist()
    {
        // Arrange
        var url = new Uri("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
        _parser.LoadDocument(url);

        // Act
        var result = _parser.ExtractAttributValue("//html", "lang");

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ExtractLinkHref_Should_ThrowException_WhenDocumentNotLoaded()
    {
        // Act
        var act = () => _parser.ExtractLinkHref("test");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractLinkHref_Should_ReturnEmptyString_WhenNoLinksFound()
    {
        // Arrange
        var parser = new TestableHtmlDataParser();
        parser.LoadHtmlContent("<html><body><p>No links here</p></body></html>");

        // Act
        var result = parser.ExtractLinkHref("test");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinkHref_Should_ReturnEmptyString_WhenPatternNotFound()
    {
        // Arrange
        var parser = new TestableHtmlDataParser();
        parser.LoadHtmlContent("<html><body><a href=\"https://example.com\">Link</a></body></html>");

        // Act
        var result = parser.ExtractLinkHref("nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinkHref_Should_ReturnLink_WhenPatternFound()
    {
        // Arrange
        var parser = new TestableHtmlDataParser();
        parser.LoadHtmlContent("<html><body><a href=\"https://example.com/test-page\">Link</a></body></html>");

        // Act
        var result = parser.ExtractLinkHref("test");

        // Assert
        result.Should().Be("https://example.com/test-page");
    }

    // Helper class to load HTML content directly for testing
    private sealed class TestableHtmlDataParser : HtmlDataParser
    {
        public void LoadHtmlContent(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            Doc = document;
        }
    }
}
