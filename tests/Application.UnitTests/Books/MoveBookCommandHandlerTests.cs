using Application.Books.Move;
using Application.Interfaces;
using Application.Libraries;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class MoveBookCommandHandlerTests
{
    private const string SourcePath = "/data/libraries/A TRIER/Blacksad T03.cbz";
    private const string TargetPath = "/data/libraries/BD/Blacksad T03.cbz";

    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly ILibraryLocalStorage _storage = Substitute.For<ILibraryLocalStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Library _source = Library.Create("À trier", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
    private readonly Library _target = Library.Create("BD", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
    private readonly DigitalBook _book;
    private readonly MoveBookCommandHandler _handler;

    public MoveBookCommandHandlerTests()
    {
        _book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), _source.Id, SourcePath, 1_000).Value!;
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _libraryRepository.GetByIdAsync(_source.Id).Returns(_source);
        _libraryRepository.GetByIdAsync(_target.Id).Returns(_target);
        _storage.MoveFile(SourcePath, _target.RelativePath).Returns(Result<string>.Success(TargetPath));
        _storage.MoveFile(TargetPath, _source.RelativePath).Returns(Result<string>.Success(SourcePath));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new MoveBookCommandHandler(_bookRepository, _libraryRepository, _storage, _unitOfWork, NullLogger<MoveBookCommandHandler>.Instance);
    }

    private Task<Result<Book>> HandleAsync(Guid targetLibraryId, Guid? userId = null) =>
        _handler.Handle(new MoveBookCommand(_book.Id, targetLibraryId, userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_MoveFileThenUpdateBook_WhenDigitalBookMovesToAnotherDigitalLibrary()
    {
        var result = await HandleAsync(_target.Id);

        result.IsSuccess.Should().BeTrue();
        _book.LibraryId.Should().Be(_target.Id);
        _book.FilePath.Should().Be(TargetPath);
        Received.InOrder(() =>
        {
            _storage.MoveFile(SourcePath, _target.RelativePath);
            _bookRepository.Update(_book);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_MoveFileBack_WhenSaveFails()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(new TError("DB", "boom")));

        var result = await HandleAsync(_target.Id);

        result.IsFailure.Should().BeTrue();
        _storage.Received(1).MoveFile(TargetPath, _source.RelativePath);
    }

    [Fact]
    public async Task Handle_Should_KeepBookUnchanged_WhenFileCannotBeMoved()
    {
        _storage.MoveFile(SourcePath, _target.RelativePath).Returns(Result<string>.Failure(BooksError.FileAlreadyExists));

        var result = await HandleAsync(_target.Id);

        result.Error.Should().Be(BooksError.FileAlreadyExists);
        _book.LibraryId.Should().Be(_source.Id);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefuseWithoutTouchingFiles_WhenTargetLibraryIsPhysical()
    {
        var physical = Library.Create("Papier", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        _libraryRepository.GetByIdAsync(physical.Id).Returns(physical);

        var result = await HandleAsync(physical.Id);

        result.Error.Should().Be(LibrariesError.BookTypeMismatch);
        _storage.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_RefuseWithoutTouchingFiles_WhenTargetIsCurrentLibrary()
    {
        var result = await HandleAsync(_source.Id);

        result.Error.Should().Be(BooksError.AlreadyInLibrary);
        _storage.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibrariesBelongToAnotherUser()
    {
        var otherUser = Guid.CreateVersion7();
        var foreignTarget = Library.Create("BD", "#5C6BC0", "Bookmark", LibraryBookType.Digital, otherUser).Value!;
        _libraryRepository.GetByIdAsync(foreignTarget.Id).Returns(foreignTarget);

        (await HandleAsync(_target.Id, otherUser)).Error.Should().Be(BooksError.NotFound);
        (await HandleAsync(foreignTarget.Id)).Error.Should().Be(LibrariesError.NotFound);
        _storage.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_MovePhysicalBookWithoutFile()
    {
        var shelf = Library.Create("Étagère", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        var otherShelf = Library.Create("Grenier", "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        var book = PhysicalBook.Create(new BookMetadata("Blacksad", "Âme rouge", "9781401245252"), shelf.Id).Value!;
        _bookRepository.GetByIdAsync(book.Id).Returns(book);
        _libraryRepository.GetByIdAsync(shelf.Id).Returns(shelf);
        _libraryRepository.GetByIdAsync(otherShelf.Id).Returns(otherShelf);

        var result = await _handler.Handle(new MoveBookCommand(book.Id, otherShelf.Id, s_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        book.LibraryId.Should().Be(otherShelf.Id);
        _storage.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenIdsAreEmpty()
    {
        var result = await _handler.Handle(new MoveBookCommand(Guid.Empty, _target.Id, s_userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }
}
