using Application.Books.Delete;
using AwesomeAssertions;
using Domain.Books;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookDeleteTextTests
{
    [Theory]
    [InlineData(1, "Delete 1 book")]
    [InlineData(4, "Delete 4 books")]
    public void Title_Should_CountTheBooks(int count, string title)
    {
        BookDeleteText.Title(count).Should().Be(title);
    }

    [Theory]
    [InlineData(1, false, "This book will be deleted with its readings. This cannot be undone.")]
    [InlineData(1, true, "This book will be deleted with its readings and its file. This cannot be undone.")]
    [InlineData(3, false, "These 3 books will be deleted with their readings. This cannot be undone.")]
    [InlineData(3, true, "These 3 books will be deleted with their readings and their files. This cannot be undone.")]
    public void Confirmation_Should_SayTheFilesAreDeletedToo_WhenTheLibraryIsDigital(int count, bool isDigital, string message)
    {
        BookDeleteText.Confirmation(count, isDigital).Should().Be(message);
    }

    [Fact]
    public void Outcome_Should_SayHowManyBooksWereDeleted_AndWhyTheOthersWereNot()
    {
        var result = new DeleteBooksResult(2, 1, BooksError.NotFound);

        BookDeleteText.Outcome(result).Should().Be($"2 of 3 books deleted. {BooksError.NotFound.Description}");
        BookDeleteText.Severity(result).Should().Be(Severity.Warning);
    }
}
