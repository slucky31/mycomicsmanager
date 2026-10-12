using Application.Books.ReaderDisplay;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.Books;

public class UpdateReaderSettingsCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly ReaderSettings s_manga = new(ReaderLayout.TwoPages, ReaderScrolling.PageByPage, ReadingDirection.RightToLeft);

    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DigitalBook _book;
    private readonly UpdateReaderSettingsCommandHandler _handler;

    public UpdateReaderSettingsCommandHandlerTests()
    {
        var library = Library.Create("Mangas", "#5C6BC0", "Bookmark", LibraryBookType.Digital, s_userId).Value!;
        _libraryRepository.GetByIdAsync(library.Id).Returns(library);
        _book = DigitalBook.Create(new BookMetadata("One Piece", "Romance Dawn", null), library.Id, "/library/Mangas/one-piece-01.cbz", 1_000).Value!;
        _bookRepository.GetByIdAsync(_book.Id).Returns(_book);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new UpdateReaderSettingsCommandHandler(_bookRepository, _libraryRepository, _unitOfWork);
    }

    public static TheoryData<bool, bool, ReaderSettings?> InvalidCommands => new()
    {
        { true, false, s_manga },
        { false, true, s_manga },
        { false, false, null },
        { false, false, new ReaderSettings(ReaderLayout.FitWidth, (ReaderScrolling)9, ReadingDirection.LeftToRight) },
        { false, false, new ReaderSettings(ReaderLayout.FitWidth, ReaderScrolling.Continuous, (ReadingDirection)9) },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Handle_Should_ReturnBadRequest_WhenParametersAreInvalid(bool emptyBookId, bool emptyUserId, ReaderSettings? settings)
    {
        var command = new UpdateReaderSettingsCommand(emptyBookId ? Guid.Empty : _book.Id, emptyUserId ? Guid.Empty : s_userId, settings!);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        await _bookRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNull()
    {
        var result = await _handler.Handle(null!, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenBookBelongsToAnotherUser()
    {
        var result = await _handler.Handle(new UpdateReaderSettingsCommand(_book.Id, Guid.CreateVersion7(), s_manga), TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.NotFound);
        _book.ReaderSettings.Should().Be(ReaderSettings.Default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SaveTheSettings_WhenTheyChanged()
    {
        var result = await _handler.Handle(new UpdateReaderSettingsCommand(_book.Id, s_userId, s_manga), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _book.ReaderSettings.Should().Be(s_manga);
        _bookRepository.Received(1).Update(_book);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSave_WhenSettingsAreUnchanged()
    {
        var result = await _handler.Handle(new UpdateReaderSettingsCommand(_book.Id, s_userId, ReaderSettings.Default), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSaveError_WhenSaveFails()
    {
        var saveError = new TError("DB500", "Save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(saveError));

        var result = await _handler.Handle(new UpdateReaderSettingsCommand(_book.Id, s_userId, s_manga), TestContext.Current.CancellationToken);

        result.Error.Should().Be(saveError);
    }
}
