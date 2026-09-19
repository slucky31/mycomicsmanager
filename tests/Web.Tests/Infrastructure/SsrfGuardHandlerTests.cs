using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class SsrfGuardHandlerTests
{
    private static HttpClient BuildClient(IReadOnlySet<string> allowedHosts, HttpMessageHandler inner)
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

    // ── Redirect following ────────────────────────────────────────────────────

    private static ResponseStep Redirect(string location, HttpStatusCode statusCode = HttpStatusCode.Found) => new(statusCode, location);

    private static readonly ResponseStep s_ok = new(HttpStatusCode.OK, null);

    [Fact]
    public async Task SendAsync_Should_FollowRedirect_WhenLocationHostIsAllowed()
    {
        var allowedHosts = new HashSet<string>(["a.example.com", "b.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://b.example.com/final"), s_ok]);
        using var client = BuildClient(allowedHosts, inner);

        var response = await client.GetAsync(new Uri("https://a.example.com/start"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Requests.Should().HaveCount(2);
        inner.Requests[1].RequestUri.Should().Be(new Uri("https://b.example.com/final"));
    }

    [Fact]
    public async Task SendAsync_Should_ResolveRelativeRedirectLocation_AgainstCurrentRequestUri()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("/final"), s_ok]);
        using var client = BuildClient(allowedHosts, inner);

        await client.GetAsync(new Uri("https://a.example.com/start"), TestContext.Current.CancellationToken);

        inner.Requests[1].RequestUri.Should().Be(new Uri("https://a.example.com/final"));
    }

    [Fact]
    public async Task SendAsync_Should_ThrowHttpRequestException_WhenRedirectTargetHostNotAllowed()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://evil.example.com/final")]);
        using var client = BuildClient(allowedHosts, inner);

        var act = () => client.GetAsync(new Uri("https://a.example.com/start"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*SSRF guard*");
        inner.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowHttpRequestException_WhenRedirectsExceedMaxRedirects()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var steps = Enumerable.Range(0, 10)
            .Select(_ => Redirect("https://a.example.com/next"))
            .ToArray();
        var inner = new SequencedInnerHandler(steps);
        using var client = BuildClient(allowedHosts, inner);

        var act = () => client.GetAsync(new Uri("https://a.example.com/start"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*too many redirects*");
        inner.Requests.Should().HaveCount(6); // MaxRedirects (5) extra hops + the initial request
    }

    [Fact]
    public async Task SendAsync_Should_StripAuthorizationHeader_WhenRedirectedToDifferentHost()
    {
        var allowedHosts = new HashSet<string>(["a.example.com", "b.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://b.example.com/final"), s_ok]);
        using var client = BuildClient(allowedHosts, inner);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://a.example.com/start"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "secret-token");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        inner.Requests[1].Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_Should_KeepAuthorizationHeader_WhenRedirectedToSameHost()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://a.example.com/final"), s_ok]);
        using var client = BuildClient(allowedHosts, inner);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://a.example.com/start"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "secret-token");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        inner.Requests[1].Headers.Authorization.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_Should_PreserveMethod_WhenRedirectIs307()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://a.example.com/final", HttpStatusCode.TemporaryRedirect), s_ok]);
        using var client = BuildClient(allowedHosts, inner);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://a.example.com/start"));

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        inner.Requests[1].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_Should_DowngradeToGet_WhenRedirectIs302()
    {
        var allowedHosts = new HashSet<string>(["a.example.com"], StringComparer.OrdinalIgnoreCase);
        var inner = new SequencedInnerHandler([Redirect("https://a.example.com/final", HttpStatusCode.Found), s_ok]);
        using var client = BuildClient(allowedHosts, inner);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://a.example.com/start"));

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        inner.Requests[1].Method.Should().Be(HttpMethod.Get);
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

    private sealed record ResponseStep(HttpStatusCode StatusCode, string? Location);

    private sealed class SequencedInnerHandler(IEnumerable<ResponseStep> steps) : HttpMessageHandler
    {
        private readonly Queue<ResponseStep> _steps = new(steps);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var step = _steps.Count > 0 ? _steps.Dequeue() : s_ok;
            var response = new HttpResponseMessage(step.StatusCode);
            if (step.Location is not null)
            {
                response.Headers.Location = new Uri(step.Location, UriKind.RelativeOrAbsolute);
            }
            return Task.FromResult(response);
        }
    }
}
