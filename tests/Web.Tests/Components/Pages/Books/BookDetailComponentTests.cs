using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class BookDetailComponentTests
{
    [Fact]
    public async Task LoadBookAsync_Should_ShowTheLoadError_WhenLoadingThrows()
    {
        var service = Substitute.For<IBooksService>();
        service.GetById(Arg.Any<string?>()).ThrowsAsync(new InvalidOperationException());
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
    }

    [Fact]
    public async Task MoveBookAsync_Should_ReloadTheBook_WhenTheBookWasMoved()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        var booksService = Substitute.For<IBooksService>();
        booksService.GetById(Arg.Any<string?>()).Returns(Result<Book>.Success(book));
        var workflow = Substitute.For<IBookMoveWorkflow>();
        workflow.ChooseAndMoveAsync(book.Id, Arg.Any<CancellationToken>()).Returns(true);
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(booksService);
        ctx.Services.AddSingleton(workflow);

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Blacksad"));
        await cut.InvokeAsync(() => cut.Instance.MoveBookAsync());

        await workflow.Received(1).ChooseAndMoveAsync(book.Id, Arg.Any<CancellationToken>());
        await booksService.Received(2).GetById(book.Id.ToString());
    }

    [Fact]
    public async Task UseIsbnCandidate_Should_OpenTheWebImportWithThePickedIsbn_WhenSeveralIsbnsWereRead()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        var booksService = Substitute.For<IBooksService>();
        booksService.GetById(Arg.Any<string?>()).Returns(Result<Book>.Success(book));
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(booksService);
        ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));
        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Several ISBNs are printed in this book"));
        await cut.FindAll("button").Single(b => b.TextContent.Contains("2205056174", StringComparison.Ordinal)).ClickAsync(new());

        ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith($"/books/{book.Id}/import?isbn=2205056174");
    }

    [Fact]
    public async Task LoadBookAsync_Should_NotOfferTheIsbnCandidates_WhenTheBookHasAnIsbn()
    {
        var book = DigitalBook.Create(new BookMetadata("Blacksad", "Âme rouge", null), Guid.CreateVersion7(), "/data/A/b.cbz", 1).Value!;
        book.RecordIsbnScan(["9782800112343", "2205056174"], DateTime.UtcNow);
        book.AssignIsbn("9782800112343");
        var booksService = Substitute.For<IBooksService>();
        booksService.GetById(Arg.Any<string?>()).Returns(Result<Book>.Success(book));
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(booksService);
        ctx.Services.AddSingleton(Substitute.For<IBookMoveWorkflow>());

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, book.Id.ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("9782800112343"));
        cut.Markup.Should().NotContain("Several ISBNs are printed in this book");
    }
}
