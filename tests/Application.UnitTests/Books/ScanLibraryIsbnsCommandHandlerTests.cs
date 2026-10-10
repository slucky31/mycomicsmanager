using Application.Books.IsbnScan;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class ScanLibraryIsbnsCommandHandlerTests
{
    private const string Isbn = "9782800112343";
    private static readonly DateTimeOffset s_now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly Guid _libraryId = Guid.CreateVersion7();
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IIsbnPageScanner _scanner = Substitute.For<IIsbnPageScanner>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();
    private readonly ScanLibraryIsbnsCommandHandler _handler;

    public ScanLibraryIsbnsCommandHandlerTests()
    {
        _clock.GetUtcNow().Returns(s_now);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new ScanLibraryIsbnsCommandHandler(_bookRepository, _scanner, _unitOfWork, _clock,
            NullLogger<ScanLibraryIsbnsCommandHandler>.Instance);
    }

    private DigitalBook AddBookToScan(string title, IsbnScanResult scan, string? isbn = null)
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", title, isbn), _libraryId, $"/data/A/{title}.cbz", 1024).Value!;
        _bookRepository.GetByIdAsync(book.Id).Returns(book);
        _scanner.ScanArchiveAsync(book.FilePath, Arg.Any<CancellationToken>()).Returns(scan);
        return book;
    }

    private void ListToScan(params Book[] books) =>
        _bookRepository.ListIdsToScanForIsbnAsync(_libraryId, Arg.Any<CancellationToken>()).Returns([.. books.Select(b => b.Id)]);

    private Task<Result<LibraryIsbnScanSummary>> ScanAsync() =>
        _handler.Handle(new ScanLibraryIsbnsCommand(_libraryId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_AssignTheIsbnAndKeepTheOtherMetadata_WhenThePagesShowASingleUnusedOne()
    {
        var book = AddBookToScan("Arctic Nation", new IsbnScanResult(true, [Isbn]));
        ListToScan(book);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(1, 0, 0, 0));
        book.ISBN.Should().Be(Isbn);
        book.Title.Should().Be("Arctic Nation");
        book.IsbnScannedAt.Should().Be(s_now.UtcDateTime);
        _bookRepository.Received(1).Update(book);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepTheIsbnAsACandidate_WhenAnotherBookAlreadyHasIt()
    {
        var book = AddBookToScan("Arctic Nation", new IsbnScanResult(true, [Isbn]));
        var other = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", Isbn), _libraryId, "/data/A/other.cbz", 1024).Value!;
        _bookRepository.GetByIsbnAsync(Isbn, Arg.Any<CancellationToken>()).Returns(other);
        ListToScan(book);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 1, 0, 0));
        book.ISBN.Should().BeNull();
        book.IsbnCandidates.Should().Equal(Isbn);
    }

    [Fact]
    public async Task Handle_Should_KeepTheIsbnsAsCandidates_WhenThePagesShowSeveral()
    {
        var book = AddBookToScan("Arctic Nation", new IsbnScanResult(true, [Isbn, "2205056174"]));
        ListToScan(book);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 1, 0, 0));
        book.ISBN.Should().BeNull();
        book.IsbnCandidates.Should().Equal(Isbn, "2205056174");
    }

    [Fact]
    public async Task Handle_Should_RecordTheScan_WhenThePagesShowNoIsbn()
    {
        var book = AddBookToScan("Arctic Nation", new IsbnScanResult(true, []));
        ListToScan(book);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 0, 1, 0));
        book.IsbnScannedAt.Should().Be(s_now.UtcDateTime);
    }

    [Fact]
    public async Task Handle_Should_LeaveTheBookToBeScannedLater_WhenItsPagesCouldNotBeScanned()
    {
        var book = AddBookToScan("Arctic Nation", IsbnScanResult.NotScanned);
        ListToScan(book);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 0, 0, 1));
        book.IsbnScannedAt.Should().BeNull();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipTheBook_WhenItGotAnIsbnOrWasScannedOrDeletedMeanwhile()
    {
        var withIsbn = AddBookToScan("Arctic Nation", new IsbnScanResult(true, [Isbn]), isbn: "2205056174");
        var scanned = AddBookToScan("Âme rouge", new IsbnScanResult(true, [Isbn]));
        scanned.RecordIsbnScan([], DateTime.UtcNow);
        var deleted = AddBookToScan("Amarillo", new IsbnScanResult(true, [Isbn]));
        _bookRepository.GetByIdAsync(deleted.Id).Returns((Book?)null);
        ListToScan(withIsbn, scanned, deleted);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 0, 0, 3));
        await _scanner.DidNotReceive().ScanArchiveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_GoOnWithTheNextBook_WhenABookCannotBeSaved()
    {
        var first = AddBookToScan("Arctic Nation", new IsbnScanResult(true, [Isbn]));
        var second = AddBookToScan("Âme rouge", new IsbnScanResult(true, []));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<int>.Failure(new TError("DB500", "Database error")), Result<int>.Success(1));
        ListToScan(first, second);

        var result = await ScanAsync();

        result.Value.Should().Be(new LibraryIsbnScanSummary(0, 0, 1, 1));
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheLibraryIdIsEmpty()
    {
        var result = await _handler.Handle(new ScanLibraryIsbnsCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LibrariesError.BadRequest);
    }
}
