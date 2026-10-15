using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Web.Components.SharedComponents;
using Xunit;

namespace Web.Tests.Components.SharedComponents;

public sealed class FormComponentsTests : IAsyncDisposable
{
    private readonly BunitContext _ctx = new();

    public FormComponentsTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    // ── FormButton ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(FormButtonVariant.Primary, "form-button-primary")]
    [InlineData(FormButtonVariant.Outlined, "form-button-outlined")]
    [InlineData(FormButtonVariant.Text, "form-button-text")]
    public void FormButton_Should_RenderAButtonOfItsVariant_WhenItHasNoHref(FormButtonVariant variant, string expectedClass)
    {
        var cut = _ctx.Render<FormButton>(p => p
            .Add(c => c.Variant, variant)
            .Add(c => c.Class, "extra")
            .AddChildContent("Save"));

        var button = cut.Find("button");
        button.GetAttribute("type").Should().Be("button");
        button.ClassList.Should().Contain(["form-button", expectedClass, "extra"]);
        button.TextContent.Should().Be("Save");
    }

    [Fact]
    public void FormButton_Should_SubmitTheNamedForm_WhenTypeIsSubmit()
    {
        var cut = _ctx.Render<FormButton>(p => p
            .Add(c => c.Type, "submit")
            .Add(c => c.Form, "add-book-form")
            .Add(c => c.Disabled, true)
            .Add(c => c.Icon, Icons.Material.Filled.Add)
            .AddChildContent("Add book"));

        var button = cut.Find("button");
        button.GetAttribute("type").Should().Be("submit");
        button.GetAttribute("form").Should().Be("add-book-form");
        button.HasAttribute("disabled").Should().BeTrue();
        button.QuerySelector("svg").Should().NotBeNull();
    }

    [Fact]
    public void FormButton_Should_RenderALink_WhenItHasAnHref()
    {
        var cut = _ctx.Render<FormButton>(p => p
            .Add(c => c.Variant, FormButtonVariant.Outlined)
            .Add(c => c.Href, "/libraries/list")
            .AddUnmatched("aria-label", "Cancel adding")
            .AddChildContent("Cancel"));

        var link = cut.Find("a");
        link.GetAttribute("href").Should().Be("/libraries/list");
        link.GetAttribute("aria-label").Should().Be("Cancel adding");
        link.ClassList.Should().Contain(["form-button", "form-button-outlined"]);
        cut.FindAll("button").Should().BeEmpty();
    }

    [Fact]
    public async Task FormButton_Should_CallOnClick_WhenClicked()
    {
        var clicks = 0;
        var cut = _ctx.Render<FormButton>(p => p
            .Add(c => c.OnClick, () => clicks++)
            .AddChildContent("Go"));

        await cut.Find("button").ClickAsync(new());

        clicks.Should().Be(1);
    }

    // ── FormHeader ────────────────────────────────────────────────────────────

    [Fact]
    public void FormHeader_Should_LinkBack_AndShowItsActions_WhenNoOnBack()
    {
        var cut = _ctx.Render<FormHeader>(p => p
            .Add(c => c.Title, "Edit book")
            .Add(c => c.Subtitle, "Extremity · Volume 1")
            .Add(c => c.BackHref, "/books/42")
            .AddChildContent("<button>Save</button>"));

        cut.Find("h1").TextContent.Should().Be("Edit book");
        cut.Find(".form-bar-subtitle").TextContent.Should().Be("Extremity · Volume 1");
        cut.Find("a[aria-label='Back']").GetAttribute("href").Should().Be("/books/42");
        cut.Find(".form-bar-actions").TextContent.Should().Be("Save");
    }

    [Fact]
    public async Task FormHeader_Should_CallOnBack_AndHideEmptyParts_WhenOnBackIsSet()
    {
        var backs = 0;
        var cut = _ctx.Render<FormHeader>(p => p
            .Add(c => c.Title, "Add a book")
            .Add(c => c.OnBack, EventCallback.Factory.Create(this, () => backs++)));

        await cut.Find("button[aria-label='Back']").ClickAsync(new());

        backs.Should().Be(1);
        cut.FindAll(".form-bar-subtitle").Should().BeEmpty();
        cut.FindAll(".form-bar-actions").Should().BeEmpty();
    }

    // ── FormPage, FormSection, FormFooter ─────────────────────────────────────

    [Fact]
    public void FormSection_Should_BeLabelledByItsTitle()
    {
        var cut = _ctx.Render<FormSection>(p => p
            .Add(c => c.Id, "book-form-book")
            .Add(c => c.Title, "Book")
            .AddChildContent("<input id=\"book-title\" />"));

        cut.Find("section").GetAttribute("aria-labelledby").Should().Be("book-form-book");
        cut.Find("h2#book-form-book").TextContent.Should().Be("Book");
        cut.Find(".form-grid #book-title").Should().NotBeNull();
    }

    [Fact]
    public void FormPage_And_FormFooter_Should_WrapTheirContent()
    {
        var page = _ctx.Render<FormPage>(p => p.Add(c => c.Class, "edit-book").AddChildContent("<p>Body</p>"));
        var footer = _ctx.Render<FormFooter>(p => p.AddChildContent("<button>Save</button>"));

        page.Find("div.form-page.edit-book p").TextContent.Should().Be("Body");
        footer.Find("div.form-footer button").TextContent.Should().Be("Save");
    }
}
