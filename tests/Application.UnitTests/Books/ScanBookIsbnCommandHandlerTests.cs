using Application.Books.IsbnScan;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public sealed class ScanBookIsbnCommandHandlerTests
{
    private const string Isbn = "9782800112343";
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly DateTimeOffset s_now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IIsbnPageScanner _scanner = Substitute.For<IIsbnPageScanner>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();
    private readonly Library _library = Library.Create("Comics", "#000000", "book", LibraryBookType.Digital, s_userId).Value!;
    private readonly ScanBookIsbnCommandHandler _handler;

    public ScanBookIsbnCommandHandlerTests()
    {
        _clock.GetUtcNow().Returns(s_now);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
        _handler = new ScanBookIsbnCommandHandler(_bookRepository, _libraryRepository, _scanner, _unitOfWork, _clock);
    }

    private DigitalBook AddBook(IsbnScanResult scan, string? isbn = null)
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", isbn), _library.Id, "/data/A/b.cbz", 1024).Value!;
        _bookRepository.GetByIdAsync(book.Id).Returns(book);
        _scanner.ScanArchiveAsync(book.FilePath, Arg.Any<CancellationToken>()).Returns(scan);
        return book;
    }

    private Task<Result<IsbnScanOutcome>> ScanAsync(Guid bookId, Guid? userId = null) =>
        _handler.Handle(new ScanBookIsbnCommand(bookId, userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ScanAgainAndAssignTheIsbn_WhenThePagesWereAlreadyScanned()
    {
        var book = AddBook(new IsbnScanResult(true, [Isbn]));
        book.RecordIsbnScan([], DateTime.UtcNow.AddDays(-3));

        var result = await ScanAsync(book.Id);

        result.Value.Should().Be(IsbnScanOutcome.Assigned);
        book.ISBN.Should().Be(Isbn);
        book.IsbnScannedAt.Should().Be(s_now.UtcDateTime);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotScanned_WithoutSaving_WhenThePagesCannotBeRead()
    {
        var book = AddBook(IsbnScanResult.NotScanned);

        var result = await ScanAsync(book.Id);

        result.Value.Should().Be(IsbnScanOutcome.NotScanned);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSaveError_WhenTheBookCannotBeSaved()
    {
        var book = AddBook(new IsbnScanResult(true, []));
        var saveError = new TError("DB500", "Database error");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(saveError));

        var result = await ScanAsync(book.Id);

        result.Error.Should().Be(saveError);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheBookAlreadyHasAnIsbn()
    {
        var book = AddBook(new IsbnScanResult(true, [Isbn]), isbn: "2205056174");

        var result = await ScanAsync(book.Id);

        result.Error.Should().Be(BooksError.BadRequest);
        await _scanner.DidNotReceive().ScanArchiveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheBookBelongsToAnotherUser()
    {
        var book = AddBook(new IsbnScanResult(true, [Isbn]));

        var result = await ScanAsync(book.Id, Guid.CreateVersion7());

        result.Error.Should().Be(BooksError.NotFound);
        await _scanner.DidNotReceive().ScanArchiveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenAnIdIsEmpty()
    {
        (await ScanAsync(Guid.Empty)).Error.Should().Be(BooksError.BadRequest);
        (await ScanAsync(Guid.CreateVersion7(), Guid.Empty)).Error.Should().Be(BooksError.BadRequest);
    }
}
