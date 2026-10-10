using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class TextIsbnExtractorTests
{
    // A copyright notice as returned by the OCR of a comic page.
    private const string CopyrightNotice = """
        Dessin : Jean Dupont - Scénario : Marie Martin
        © DUPUIS 2024, by Dupont, Martin.
        Dépôt légal : mars 2024 - D.2024/0089/123
        ISBN 978-2-8001-1234-3
        Imprimé en Belgique par Lesaffre.
        """;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractAll_Should_ReturnEmpty_WhenTextIsEmpty(string? text)
    {
        TextIsbnExtractor.ExtractAll(text).Should().BeEmpty();
    }

    [Fact]
    public void ExtractAll_Should_ReturnTheNormalizedIsbn_WhenTheCopyrightNoticeHasOne()
    {
        TextIsbnExtractor.ExtractAll(CopyrightNotice).Should().Equal("9782800112343");
    }

    [Theory]
    [InlineData("ISBN-13 : 978-2-8001-1234-3")]
    [InlineData("ISBN 13 9782800112343")]
    [InlineData("isbn:978 2 8001 1234 3")]
    [InlineData("I S B N 978.2-8001-1234-3")]
    [InlineData("ISBN 978\u20132\u20138001\u20131234\u20133")]
    [InlineData("ISBN\n978-2-8001-1234-3")]
    public void ExtractAll_Should_ReadTheIsbn_WhenTheLabelOrSeparatorsVary(string text)
    {
        TextIsbnExtractor.ExtractAll(text).Should().Equal("9782800112343");
    }

    [Theory]
    [InlineData("ISBN 978-2-8OO1-l234-3")]
    [InlineData("ISBN 978-2-BOO1-1Z34-3")]
    [InlineData("ISBN 97B-2-8001-|234-3")]
    public void ExtractAll_Should_FixDigitLookalikes_WhenTheyFollowTheIsbnLabel(string text)
    {
        TextIsbnExtractor.ExtractAll(text).Should().Equal("9782800112343");
    }

    [Fact]
    public void ExtractAll_Should_ReadAnIsbn10_WhenTheLabelIntroducesOne()
    {
        TextIsbnExtractor.ExtractAll("ISBN-10 : 2-205-05617-4 Imprimé en France").Should().Equal("2205056174");
    }

    [Fact]
    public void ExtractAll_Should_KeepTheIsbn10CheckDigit_WhenItIsAnX()
    {
        TextIsbnExtractor.ExtractAll("ISBN 2-8001-1231-x 2024").Should().Equal("280011231X");
    }

    [Theory]
    [InlineData("ISBN 978-2-8001-1234-4")]
    [InlineData("ISBN 2-205-05617-5")]
    public void ExtractAll_Should_ReturnEmpty_WhenTheChecksumIsWrong(string text)
    {
        TextIsbnExtractor.ExtractAll(text).Should().BeEmpty();
    }

    [Fact]
    public void ExtractAll_Should_NotFallBackToAnIsbn10_WhenAnIsbn13IsMisread()
    {
        // "9782800112" is a valid ISBN-10, but it is only the beginning of a misread ISBN-13.
        TextIsbnExtractor.ExtractAll("ISBN 978-2-8001-1200-9").Should().BeEmpty();
    }

    [Fact]
    public void ExtractAll_Should_ReadTheIsbn13_WhenItIsPrintedUnderTheBarcodeWithoutLabel()
    {
        TextIsbnExtractor.ExtractAll("9 782800 112343").Should().Equal("9782800112343");
    }

    [Fact]
    public void ExtractAll_Should_ReadAHyphenatedIsbn10_WhenItHasNoLabel()
    {
        TextIsbnExtractor.ExtractAll("Déjà paru : 2-205-05617-4").Should().Equal("2205056174");
    }

    [Fact]
    public void ExtractAll_Should_IgnoreABareNumber_WhenItIsNeitherAnIsbn13NorHyphenated()
    {
        // A valid ISBN-10 checksum, but without label nor hyphens it may be any number (a phone number here).
        TextIsbnExtractor.ExtractAll("Tél. 0123456703").Should().BeEmpty();
    }

    [Fact]
    public void ExtractAll_Should_ReturnEachIsbnOnce_WhenItIsPrintedTwice()
    {
        var text = CopyrightNotice + "\n9 782800 112343";

        TextIsbnExtractor.ExtractAll(text).Should().Equal("9782800112343");
    }

    [Fact]
    public void ExtractAll_Should_ListTheLabelledIsbnFirst_WhenThePageHasSeveral()
    {
        const string Text = "Intégrale : 2-205-05617-4\nISBN 978-2-8001-1234-3";

        TextIsbnExtractor.ExtractAll(Text).Should().Equal("9782800112343", "2205056174");
    }
}
