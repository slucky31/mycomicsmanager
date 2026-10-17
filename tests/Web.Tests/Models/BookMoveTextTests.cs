using Application.Books.Move;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookMoveTextTests
{
    [Theory]
    [InlineData(1, "Move 1 book")]
    [InlineData(4, "Move 4 books")]
    public void Title_Should_CountTheBooks(int count, string title)
    {
        BookMoveText.Title(count).Should().Be(title);
    }

    public static TheoryData<int, int, string?, string, Severity> Outcomes => new()
    {
        { 1, 0, null, "1 book moved", Severity.Success },
        { 3, 0, null, "3 books moved", Severity.Success },
        { 2, 1, "The book file was not found", "2 of 3 books moved. The book file was not found", Severity.Warning },
        { 0, 2, "The book file was not found", "No book was moved. The book file was not found", Severity.Error },
        { 0, 2, null, "No book was moved", Severity.Error },
    };

    [Theory]
    [MemberData(nameof(Outcomes))]
    public void Outcome_Should_SayHowManyBooksWereMoved_AndWhyTheOthersWereNot(int moved, int failed, string? reason, string outcome, Severity severity)
    {
        var result = new MoveBooksResult(moved, failed, reason is null ? null : new TError(BooksError.FileNotFound.Code, reason));

        BookMoveText.Outcome(result).Should().Be(outcome);
        BookMoveText.Severity(result).Should().Be(severity);
    }
}
