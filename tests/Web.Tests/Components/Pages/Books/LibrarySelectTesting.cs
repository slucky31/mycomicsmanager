using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Infrastructure;
using Web.Services;

namespace Web.Tests.Components.Pages.Books;

// What a page showing a LibrarySelect needs in a test, and the two taps to choose a library.
internal static class LibrarySelectTesting
{
    public static BunitJSModuleInterop AddLibrarySelect(this BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule(AppVersion.Versioned("./js/popupAnchor.js"));
        module.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddScoped<PopupAnchorLocator>();
        return module;
    }

    public static async Task ChooseLibraryAsync<TComponent>(this IRenderedComponent<TComponent> cut, string selectId, string libraryName)
        where TComponent : IComponent
    {
        await cut.Find($"#{selectId}").ClickAsync(new());
        await cut.Find($"[role=dialog] .action-sheet-item:contains('{libraryName}')").ClickAsync(new());
    }
}
