using System.Net;
using AwesomeAssertions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class SsrfGuardHandlerTests
{
    private static HttpClient BuildClient(IReadOnlySet<string> allowedHosts, FakeInnerHandler inner)
    {
        // CA2000 suppressed: HttpClient takes ownership of the handler and disposes it
#pragma warning disable CA2000
        var guard = new SsrfGuardHandler(allowedHosts) { InnerHandler = inner };
#pragma warning restore CA2000
        return new HttpClient(guard);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowHttpRequestException_WhenSchemeIsHttp()
    {
        var inner = new FakeInnerHandler();
        using var client = BuildClient(
            new HashSet<string>(["openlibrary.org"], StringComparer.OrdinalIgnoreCase), inner);

        var act = () => client.GetAsync(new Uri("http://openlibrary.org/isbn/123.json"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*SSRF guard*");
        inner.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_Should_ThrowHttpRequestException_WhenHostNotInAllowList()
    {
        var inner = new FakeInnerHandler();
        using var client = BuildClient(
            new HashSet<string>(["openlibrary.org"], StringComparer.OrdinalIgnoreCase), inner);

        var act = () => client.GetAsync(new Uri("https://evil.internal/metadata"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*SSRF guard*");
        inner.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_Should_ForwardRequest_WhenSchemeIsHttpsAndHostIsAllowed()
    {
        var inner = new FakeInnerHandler();
        using var client = BuildClient(
            new HashSet<string>(["openlibrary.org"], StringComparer.OrdinalIgnoreCase), inner);

        await client.GetAsync(new Uri("https://openlibrary.org/isbn/123.json"),
            TestContext.Current.CancellationToken);

        inner.WasCalled.Should().BeTrue();
    }

    private sealed class FakeInnerHandler : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
