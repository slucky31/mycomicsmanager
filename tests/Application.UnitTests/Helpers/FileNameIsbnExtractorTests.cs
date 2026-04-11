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

    // ── Real-world filename patterns ──────────────────────────────────────────

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenRawIsbnAppearsAfterTitle()
    {
        // "Soda T01 9782800116136" — raw 13-digit sequence after title and volume
        var result = FileNameIsbnExtractor.ExtractIsbn("Soda T01 9782800116136.cbz");
        result.Should().Be("9782800116136");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIsbnPrefixHasDashes()
    {
        // "Blacksad ISBN-978-2-07-516286-9" — explicit ISBN keyword with dashes in value
        var result = FileNameIsbnExtractor.ExtractIsbn("Blacksad ISBN-978-2-07-516286-9.cbr");
        result.Should().Be("9782075162869");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnIsbn_WhenIsbnInParenthesesAmongOtherNumbers()
    {
        // "Mon comic [2024] (9782800116136)" — false positive in brackets, valid ISBN in parens
        var result = FileNameIsbnExtractor.ExtractIsbn("Mon comic [2024] (9782800116136).pdf");
        result.Should().Be("9782800116136");
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnNull_WhenOnlyRandomNumbers()
    {
        // "Comic 2024 Edition 12345" — short numbers, none matching ISBN format
        var result = FileNameIsbnExtractor.ExtractIsbn("Comic 2024 Edition 12345.cbz");
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractIsbn_Should_ReturnFirstValidIsbn_WhenMultipleValidIsbnsPresent()
    {
        // "9782075162869 Comic 9782800116136" — both are valid; first match wins
        var result = FileNameIsbnExtractor.ExtractIsbn("9782075162869 Comic 9782800116136.cbz");
        result.Should().Be("9782075162869");
    }
}
