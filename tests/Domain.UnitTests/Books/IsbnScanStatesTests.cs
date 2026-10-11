using Domain.Books;

namespace Domain.UnitTests.Books;

public class IsbnScanStatesTests
{
    private static readonly DateTime s_scannedAt = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string?, int, DateTime?, IsbnScanState> States => new()
    {
        { "9782800112343", 0, null, IsbnScanState.HasIsbn },
        { "9782800112343", 2, s_scannedAt, IsbnScanState.HasIsbn },
        { null, 0, null, IsbnScanState.NotScanned },
        { " ", 0, null, IsbnScanState.NotScanned },
        { null, 2, s_scannedAt, IsbnScanState.Candidates },
        { null, 0, s_scannedAt, IsbnScanState.NotFound },
    };

    [Theory]
    [MemberData(nameof(States))]
    public void Of_Should_TellWhereTheSearchOfTheIsbnStands(string? isbn, int candidateCount, DateTime? scannedAt, IsbnScanState expected)
    {
        IsbnScanStates.Of(isbn, candidateCount, scannedAt).Should().Be(expected);
    }

    [Fact]
    public void GetIsbnScanState_Should_ReflectTheScanOfTheBook()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        book.GetIsbnScanState().Should().Be(IsbnScanState.NotScanned);

        book.RecordIsbnScan(["9782800112343", "2205056174"], s_scannedAt);

        book.GetIsbnScanState().Should().Be(IsbnScanState.Candidates);
    }
}
