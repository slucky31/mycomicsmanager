using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class FileNameIsbnExtractorTests
{
    // ── ISBN prefix patterns ──────────────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIsbnPrefixPresent()
    {
        // "ISBN-9782075162869" — dash separator, valid ISBN-13
        var result = FileNameIsbnExtractor.ExtractIsbn("My Comic ISBN-9782075162869.cbz");
        result.Should().Be("9782075162869");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIsbnUnderscoreSeparator()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("My Comic ISBN_9782075162869.cbz");
        result.Should().Be("9782075162869");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIsbnSpaceSeparator()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("My Comic ISBN 9782075162869.cbz");
        result.Should().Be("9782075162869");
    }

    // ── Parentheses ───────────────────────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenInParentheses()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("Serie - T01 (9782075162869).cbz");
        result.Should().Be("9782075162869");
    }

    [Fact]
    public void ExtractIsbn_Should_HandleHyphenatedIsbn()
    {
        // Hyphenated ISBN-13 between parentheses
        var result = FileNameIsbnExtractor.ExtractIsbn("Serie (978-2-07-516286-9).cbz");
        result.Should().Be("9782075162869");
    }

    // ── Brackets ─────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenInBrackets()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("Serie T01 [9782075162869].cbz");
        result.Should().Be("9782075162869");
    }

    // ── Raw digit sequences ───────────────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIs13DigitSequence()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("9782075162869 Serie T01.cbz");
        result.Should().Be("9782075162869");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIs10DigitSequence()
    {
        // Valid ISBN-10: 0306406152
        var result = FileNameIsbnExtractor.ExtractIsbn("Old Comic 0306406152.cbz");
        result.Should().Be("0306406152");
    }

    // ── No match / invalid ────────────────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnNull_WhenNoIsbnFound()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn("My Comic Without Any Isbn.cbz");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnNull_WhenInvalidIsbn()
    {
        // 13 digits starting with 978 but wrong check digit → not valid
        var result = FileNameIsbnExtractor.ExtractIsbn("Bad Comic 9782075162860.cbz");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnNull_WhenFileNameIsEmpty()
    {
        var result = FileNameIsbnExtractor.ExtractIsbn(string.Empty);
        result.Should().BeNull();
    }
}
