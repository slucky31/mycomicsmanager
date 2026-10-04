using AwesomeAssertions;
using Bunit;
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

        var cut = ctx.Render<BookDetail>(p => p.Add(c => c.BookId, Guid.CreateVersion7().ToString()));

        await cut.WaitForAssertionAsync(() => cut.Markup.Should().Contain("Book not found"));
    }
}
