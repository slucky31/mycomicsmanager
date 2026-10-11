using Application.Books.ReadingProgress;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class UpdateReadingProgressCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DigitalBook _book;
    private readonly UpdateReadingProgressCommandHandler _handler;

    public UpdateReadingProgressCommandHandlerTests()
    {
        var library = Library.Create("BD", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        _book = DigitalBook.Create(new BookMetadata("Blacksad", "Arctic Nation", null), library.Id, "/library/BD/blacksad-02.cbz", 1_000).Value!;
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new UpdateReadingProgressCommandHandler(_bookRepository, _libraryRepository, _unitOfWork);
    }

    [Theory]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(false, false, -1)]
    public async Task Handle_Should_ReturnBadRequest_WhenParametersAreInvalid(bool emptyBookId, bool emptyUserId, int pageIndex)
    {
        var command = new UpdateReadingProgressCommand(emptyBookId ? Guid.Empty : _book.Id, emptyUserId ? Guid.Empty : s_userId, pageIndex);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToAnotherUser()
    {
        var result = await _handler.Handle(new UpdateReadingProgressCommand(_book.Id, Guid.CreateVersion7(), 3), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
        _book.LastReadPage.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SaveProgress_WhenPageChanged()
    {
        var result = await _handler.Handle(new UpdateReadingProgressCommand(_book.Id, s_userId, 3), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _book.LastReadPage.Should().Be(3);
        _bookRepository.Received(1).Update(_book);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSave_WhenPageIsUnchanged()
    {
        _book.UpdateReadingProgress(3);

        var result = await _handler.Handle(new UpdateReadingProgressCommand(_book.Id, s_userId, 3), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSaveError_WhenSaveFails()
    {
        var saveError = new TError("DB500", "Save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(saveError));

        var result = await _handler.Handle(new UpdateReadingProgressCommand(_book.Id, s_userId, 3), TestContext.Current.CancellationToken);

        result.Error.Should().Be(saveError);
    }
}
