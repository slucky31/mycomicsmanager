using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class IsbnHelperTests
{

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("12345", false)]
    [InlineData("12345678901", false)]
    [InlineData("123456789012", false)]
    [InlineData("978-0-306-40615-77", false)]
    [InlineData("978-3-16-148410-1", false)]
    [InlineData("0-306-40615-2", true)]
    [InlineData("0306406152", true)]
    [InlineData("0 306 40615 2", true)]
    [InlineData("080442957X", true)]
    [InlineData("0-8044-2957-X", true)]
    [InlineData("080442957x", true)]
    [InlineData("043942089X", true)]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("9780306406157", true)]
    [InlineData("978 0 306 40615 7", true)]
    [InlineData("978 0-306-40615 7", true)]
    [InlineData("978  0  306  40615  7", true)]
    [InlineData("978-3-16-148410-0", true)]
    [InlineData("invalid-isbn", false)]
    [InlineData("123", false)]
    [InlineData("12345678901234", false)]
    public void IsValidISBN_ShouldReturnExpectedValue_WhenIsbnIsprovided(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0306406152", true)]
    [InlineData("0345339681", true)]
    [InlineData("0684801221", true)]
    [InlineData("080442957X", true)]
    [InlineData("043942089X", true)]
    [InlineData("03064A6152", false)]
    [InlineData("0X06406152", false)]
    [InlineData("030640615Y", false)]
    [InlineData("0306406153", false)]
    public void IsValidISBN10_ShouldReturnExpectedValue_WhenIsbnIsprovided(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("9780306406157", true)]
    [InlineData("9783161484100", true)]
    [InlineData("9780451524935", true)]
    [InlineData("9780743273565", true)]
    [InlineData("978030640615A", false)]
    [InlineData("9780306406158", false)]
    [InlineData("978-0306406157", false)]
    public void IsValidISBN13_ShouldReturnExpectedValue_WhenIsbnIsprovided(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }


    // ToHyphenatedIsbn ─────────────────────────────────────────────────────────

    [Theory]
    // wrong length
    [InlineData("978030640615",   null)]  // 12 chars
    [InlineData("97803064061577", null)]  // 14 chars
    // unsupported prefix
    [InlineData("9780306406157",  null)]  // 978-0
    [InlineData("9783161484100",  null)]  // 978-3
    [InlineData("9791100000000",  null)]  // 979-11
    // 979-10 publisher range not covered (975000-999999 is a gap in the implementation)
    [InlineData("9791097500000",  null)]
    // non-digit characters in body — must not throw
    [InlineData("9782A05071153",  null)]
    [InlineData("979100A000000",  null)]
    // 978-2 publisher length 2 (p2 ≤ 19)
    [InlineData("9782010031601",  "978-2-01-003160-1")]
    // 978-2 publisher length 3 (p3 ∈ 200-699)
    [InlineData("9782205071153",  "978-2-205-07115-3")]
    // 978-2 publisher length 4 (p4 ∈ 7000-8499)
    [InlineData("9782750000000",  "978-2-7500-0000-0")]
    // 978-2 publisher length 5 (p5 ∈ 85000-89999)
    [InlineData("9782850000000",  "978-2-85000-000-0")]
    // 978-2 publisher length 6 (p6 ∈ 900000-949999)
    [InlineData("9782900000000",  "978-2-900000-00-0")]
    // 978-2 publisher length 7 (p7 ∈ 9500000-9999999)
    [InlineData("9782950000000",  "978-2-9500000-0-0")]
    // 979-10 publisher length 2 (p2 ≤ 19)
    [InlineData("9791010000000",  "979-10-10-00000-0")]
    // 979-10 publisher length 3 (p3 ∈ 200-699)
    [InlineData("9791020000000",  "979-10-200-0000-0")]
    // 979-10 publisher length 4 (p4 ∈ 7000-8699)
    [InlineData("9791070000000",  "979-10-7000-000-0")]
    // 979-10 publisher length 5 (p5 ∈ 87000-89999)
    [InlineData("9791087000000",  "979-10-87000-00-0")]
    // 979-10 publisher length 6 (p6 ∈ 900000-974999)
    [InlineData("9791090000000",  "979-10-900000-0-0")]
    public void ToHyphenatedIsbn_Should_ReturnExpectedValue_WhenIsbnIsProvided(string isbn, string? expected)
    {
        var result = IsbnHelper.ToHyphenatedIsbn(isbn);

        result.Should().Be(expected);
    }

    // ToShortIsbn ───────────────────────────────────────────────────────────────

    [Theory]
    // wrong length
    [InlineData("97803064061",    null)]  // 11 chars
    [InlineData("97803064061577", null)]  // 14 chars
    // non-978/979 prefix
    [InlineData("1234567890128",  null)]
    // 978 prefix
    [InlineData("9780306406157",  "0-306-40615")]
    [InlineData("9782205071153",  "2-205-07115")]
    // 979 prefix
    [InlineData("9791097500000",  "1-097-50000")]
    public void ToShortIsbn_Should_ReturnExpectedValue_WhenIsbnIsProvided(string isbn, string? expected)
    {
        var result = IsbnHelper.ToShortIsbn(isbn);

        result.Should().Be(expected);
    }

    // NormalizeIsbn ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("978 0 306 40615 7", "9780306406157")]
    [InlineData("978 0-306-40615 7", "9780306406157")]
    [InlineData("978--0--306--40615--7", "9780306406157")]
    [InlineData("978  0  306  40615  7", "9780306406157")]
    [InlineData("9780306406157", "9780306406157")]
    [InlineData("080442957x", "080442957X")]
    [InlineData("0-8044-2957-x", "080442957X")]
    [InlineData("abcde12345", "ABCDE12345")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0-8044-2957-X", "080442957X")]
    [InlineData("978  0--306  40615--7", "9780306406157")]
    public void NormalizeIsbn_ShouldReturnExpectedValue_WhenIsbnIsprovided(string? isbn, string expected)
    {
        // Act
        var result = IsbnHelper.NormalizeIsbn(isbn!);

        // Assert
        result.Should().Be(expected);
    }

}
