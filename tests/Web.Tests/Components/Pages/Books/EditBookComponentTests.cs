using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Components.Pages.Dialogs;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class EditBookComponentTests : IAsyncDisposable
{
    private const string Isbn = "9782413015406";

    private readonly BunitContext _ctx = new();
    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly BunitNavigationManager _navigation;
    private readonly Library _library = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
    private readonly PhysicalBook _book;
    private readonly ReadingDate _recentReading;

    public EditBookComponentTests()
    {
        _book = PhysicalBook.Create(new BookMetadata("Extremity", "Extremity", Isbn, 1, "/covers/extremity.webp", "Daniel Warren Johnson", "Delcourt", new DateOnly(2019, 4, 3), 271), _library.Id).Value!;
        _book.AddReadingDate(new DateTime(2024, 8, 2, 0, 0, 0, DateTimeKind.Utc), 5);
        _recentReading = _book.AddReadingDate(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc), 4);

        _booksService.GetById(Arg.Any<string?>()).Returns(_ => Result<Book>.Success(_book));
        _booksService.Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Result<Book>.Success(_book));
        _booksService.DeleteReadingDate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _librariesService.GetById(Arg.Any<string?>()).Returns(_ => Result<Library>.Success(_library));
        ConfirmDialogsWith(DialogResult.Ok(true));

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton(_booksService);
        _ctx.Services.AddSingleton(_librariesService);
        _ctx.Services.AddSingleton(_dialogs);
        _ctx.Services.AddSingleton(_snackbar);
        _navigation = _ctx.Services.GetRequiredService<BunitNavigationManager>();
    }

    public async ValueTask DisposeAsync()
    {
        _snackbar.Dispose();
        await _ctx.DisposeAsync();
    }

    private void ConfirmDialogsWith(DialogResult result)
    {
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(result);
        _dialogs.ShowAsync<ConfirmationDialog>(Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>()).Returns(dialog);
    }

    private IRenderedComponent<EditBook> Render() => _ctx.Render<EditBook>(p => p.Add(c => c.BookId, _book.Id.ToString()));

    private static Task TypeAsync(IRenderedComponent<EditBook> cut, string id, string value) =>
        cut.Find($"#{id}").InputAsync(new() { Value = value });

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public void EditBook_Should_ShowBookNotFound_WhenTheBookCannotBeLoaded(Type? exceptionType)
    {
        if (exceptionType is null)
        {
            _booksService.GetById(Arg.Any<string?>()).Returns(BooksError.NotFound);
        }
        else
        {
            _booksService.GetById(Arg.Any<string?>()).ThrowsAsync((Exception)Activator.CreateInstance(exceptionType)!);
        }

        var cut = Render();

        cut.Find(".edit-state h2").TextContent.Should().Be("Book not found");
        cut.Find(".edit-state a").GetAttribute("href").Should().Be("/libraries/list");
        _snackbar.Received(1).Add("Unable to load the book", Severity.Error);
    }

    [Fact]
    public void EditBook_Should_ShowTheBook_ItsLibrary_AndItsReadings()
    {
        var cut = Render();

        cut.Find("h1").TextContent.Should().Be("Edit book");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Extremity · Volume 1");
        cut.Find(".form-bar-back").GetAttribute("href").Should().Be($"/books/{_book.Id}");
        cut.Find("#book-title").GetAttribute("value").Should().Be("Extremity");
        cut.Find("#book-pages").GetAttribute("value").Should().Be("271");
        cut.Find("label[for=book-isbn]").TextContent.Should().Be("ISBN *");
        cut.Find(".edit-library-name").TextContent.Should().Be("Read Books · Physical");
        cut.Find(".edit-library-cover").GetAttribute("alt").Should().Be("Cover of Extremity");
        cut.Find(".form-section-note").TextContent.Should().Be("2 readings");
        cut.FindAll(".edit-reading-date").Select(d => d.TextContent).Should().Equal("12 March 2026", "2 August 2024");
        cut.Find(".edit-reading [role=img]").GetAttribute("aria-label").Should().Be("Rated 4 out of 5");
        cut.Find(".edit-reading-delete").GetAttribute("aria-label").Should().Be("Delete the reading of 12 March 2026");
    }

    [Fact]
    public void EditBook_Should_NameTheLibraryLibrary_WhenItCannotBeLoaded_AndSayThereIsNoReading()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null, 4), _library.Id, "/data/A/b.cbz", 1).Value!;
        _booksService.GetById(Arg.Any<string?>()).Returns(_ => Result<Book>.Success(book));
        _librariesService.GetById(Arg.Any<string?>()).Returns(LibrariesError.NotFound);

        var cut = Render();

        cut.Find(".edit-library-name").TextContent.Should().Be("Library · Digital");
        cut.Find("label[for=book-isbn]").TextContent.Should().Be("ISBN");
        cut.Find(".edit-readings-empty").TextContent.Should().Be("No readings recorded");
        cut.FindAll(".edit-library-cover").Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_Should_SaveTheChanges_AndReturnToTheBook()
    {
        var cut = Render();

        await TypeAsync(cut, "book-title", "Extremity: Artist");
        await cut.Find("#book-volume").ChangeAsync(new() { Value = "2" });
        await cut.Find("form").SubmitAsync();

        await _booksService.Received(1).Update(
            Arg.Is<UpdateBookRequest>(r => r.Id == _book.Id.ToString() && r.Title == "Extremity: Artist" && r.VolumeNumber == 2 && r.Isbn == Isbn && r.NumberOfPages == 271),
            Arg.Any<CancellationToken>());
        _snackbar.Received(1).Add("Extremity: Artist was saved", Severity.Success);
        _navigation.Uri.Should().EndWith($"/books/{_book.Id}");
    }

    [Fact]
    public async Task SaveAsync_Should_ShowTheErrorsUnderTheFields_AndFollowWhatIsTyped()
    {
        var cut = Render();
        await TypeAsync(cut, "book-title", "");
        await TypeAsync(cut, "book-isbn", "123");

        await cut.Find("form").SubmitAsync();

        cut.Find("[role=alert]").TextContent.Should().Be("2 fields need your attention.");
        cut.Find("#book-title-error").TextContent.Should().Be("Enter a title");
        cut.Find("#book-isbn").GetAttribute("aria-invalid").Should().Be("true");
        await _booksService.DidNotReceive().Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>());

        await TypeAsync(cut, "book-title", "Extremity");

        cut.Find("[role=alert]").TextContent.Should().Be("1 field needs your attention.");
    }

    public static TheoryData<TError, string> SaveFailures => new()
    {
        { BooksError.Duplicate, "Another book already has this ISBN" },
        { BooksError.NotFound, "Unable to save the changes" },
    };

    [Theory]
    [MemberData(nameof(SaveFailures))]
    public async Task SaveAsync_Should_SayWhyTheChangesWereNotSaved_AndStayOnTheForm(TError error, string message)
    {
        _booksService.Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Result<Book>.Failure(error));
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add(message, Severity.Error);
        _navigation.Uri.Should().NotEndWith($"/books/{_book.Id}");
        cut.Find(".form-footer button[type=submit]").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_Should_SayTheChangesWereNotSaved_WhenTheSaveFails()
    {
        _booksService.Update(Arg.Any<UpdateBookRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db"));
        var cut = Render();

        await cut.Find("form").SubmitAsync();

        _snackbar.Received(1).Add("Unable to save the changes", Severity.Error);
    }

    [Fact]
    public async Task DeleteReadingAsync_Should_DeleteTheReadingAfterConfirmation_AndKeepTheFieldsBeingEdited()
    {
        var cut = Render();
        await TypeAsync(cut, "book-title", "Changed");
        _book.RemoveReadingDate(_recentReading.Id);

        await cut.Find(".edit-reading-delete").ClickAsync(new());

        await _booksService.Received(1).DeleteReadingDate(_book.Id.ToString(), _recentReading.Id.ToString(), Arg.Any<CancellationToken>());
        await _dialogs.Received(1).ShowAsync<ConfirmationDialog>("Delete this reading?", Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>());
        cut.FindAll(".edit-reading-date").Select(d => d.TextContent).Should().Equal("2 August 2024");
        cut.Find(".form-section-note").TextContent.Should().Be("1 reading");
        cut.Find("#book-title").GetAttribute("value").Should().Be("Changed");
    }

    [Fact]
    public async Task DeleteReadingAsync_Should_KeepTheReading_WhenTheDeletionIsCancelled()
    {
        ConfirmDialogsWith(DialogResult.Cancel());
        var cut = Render();

        await cut.Find(".edit-reading-delete").ClickAsync(new());

        await _booksService.DidNotReceive().DeleteReadingDate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        cut.FindAll(".edit-reading").Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteReadingAsync_Should_SayTheReadingWasNotDeleted_WhenTheDeletionFails()
    {
        _booksService.DeleteReadingDate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Failure(BooksError.NotFound));
        var cut = Render();

        await cut.Find(".edit-reading-delete").ClickAsync(new());

        _snackbar.Received(1).Add("Unable to delete the reading", Severity.Error);
        cut.FindAll(".edit-reading").Should().HaveCount(2);
        cut.Find(".edit-reading-delete").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task DeleteReadingAsync_Should_SayTheReadingWasNotDeleted_WhenTheDeletionThrows()
    {
        _booksService.DeleteReadingDate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var cut = Render();

        await cut.Find(".edit-reading-delete").ClickAsync(new());

        _snackbar.Received(1).Add("Unable to delete the reading", Severity.Error);
    }
}
