using AwesomeAssertions;
using Bunit;
using Domain.Books;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Web.Components.Pages.Books;
using Web.Services;
using Xunit;

namespace Web.Tests.Components.Pages.Books;

public sealed class EditBookComponentTests
{
    public static TheoryData<Exception?, string, Severity> LoadFailures => new()
    {
        { null, "Book not found.", Severity.Error },
        { new OperationCanceledException(), "The operation was cancelled.", Severity.Warning },
        { new InvalidOperationException(), "An unexpected error occurred while loading the book.", Severity.Error },
    };

    [Theory]
    [MemberData(nameof(LoadFailures))]
    public async Task LoadBookAsync_Should_ShowBookNotFound_WhenLoadingFails(
        Exception? exception, string message, Severity severity)
    {
        var service = Substitute.For<IBooksService>();
        if (exception is null)
        {
            service.GetById(Arg.Any<string?>()).Returns(BooksError.NotFound);
        }
        else
        {
            service.GetById(Arg.Any<string?>()).ThrowsAsync(exception);
        }
        var snackbar = Substitute.For<ISnackbar>();
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton(service);
        ctx.Services.AddSingleton(snackbar);

        var cut = ctx.Render<EditBook>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
        snackbar.Received(1).Add(message, severity);
    }
}
