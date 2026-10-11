using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Books.ReadingProgress;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class BookReaderServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetBookReaderInfoQuery, BookReaderInfoDto> _infoHandler = Substitute.For<IQueryHandler<GetBookReaderInfoQuery, BookReaderInfoDto>>();
    private readonly ICommandHandler<UpdateReadingProgressCommand> _progressHandler = Substitute.For<ICommandHandler<UpdateReadingProgressCommand>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly BookReaderService _service;

    public BookReaderServiceTests()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _service = new BookReaderService(_infoHandler, _progressHandler, _currentUserService);
    }

    [Fact]
    public async Task GetReaderInfoAsync_Should_QueryForCurrentUser()
    {
        var bookId = Guid.CreateVersion7();
        var info = new BookReaderInfoDto(bookId, "Blacksad", "Arctic Nation", 2, 48, 5);
        _infoHandler.Handle(new GetBookReaderInfoQuery(bookId, s_userId), Arg.Any<CancellationToken>()).Returns(info);

        var result = await _service.GetReaderInfoAsync(bookId, TestContext.Current.CancellationToken);

        result.Value.Should().Be(info);
    }

    [Fact]
    public async Task SaveProgressAsync_Should_SendCommandForCurrentUser()
    {
        var bookId = Guid.CreateVersion7();
        _progressHandler.Handle(Arg.Any<UpdateReadingProgressCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _service.SaveProgressAsync(bookId, 12, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _progressHandler.Received(1).Handle(new UpdateReadingProgressCommand(bookId, s_userId, 12), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveProgressAsync_Should_NotSendCommand_WhenUserIsUnknown()
    {
        var userError = new TError("USR404", "User not found");
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(userError));

        var result = await _service.SaveProgressAsync(Guid.CreateVersion7(), 12, TestContext.Current.CancellationToken);

        result.Error.Should().Be(userError);
        await _progressHandler.DidNotReceive().Handle(Arg.Any<UpdateReadingProgressCommand>(), Arg.Any<CancellationToken>());
    }
}
