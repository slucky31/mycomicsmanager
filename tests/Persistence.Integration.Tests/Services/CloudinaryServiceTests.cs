using System.Net;
using System.Reflection;
using System.Text;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Domain.Books;
using Microsoft.Extensions.Options;
using Persistence.Services;

namespace Persistence.Tests.Services;

public class CloudinaryServiceTests
{
    private static CloudinaryService CreateServiceWithMockHandler(HttpMessageHandler handler)
    {
        var settings = Options.Create(new CloudinarySettings
        {
            CloudName = "testcloud",
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret"
        });

        var service = new CloudinaryService(settings, NullLogger<CloudinaryService>.Instance);

        // Inject mock HttpClient via reflection: service._cloudinary.Api.Client
        var cloudinaryField = typeof(CloudinaryService)
            .GetField("_cloudinary", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cloudinary = cloudinaryField.GetValue(service)!;
        var api = cloudinary.GetType().GetProperty("Api")!.GetValue(cloudinary)!;
        // CA2000 suppressed: ownership of HttpClient is transferred to the cloudinary API field
#pragma warning disable CA2000
        api.GetType().GetField("Client")!.SetValue(api, new HttpClient(handler));
#pragma warning restore CA2000

        return service;
    }

    [Theory]
    [InlineData("https://covers.openlibrary.org/b/id/123-L.jpg")]
    [InlineData("https://assets.bubblebd.com/img/zwisd1reza/tj08p3szc3.jpg")]
    public async Task UploadImageFromUrlAsync_ShouldReturnSuccess_WhenUploadSucceeds(string address)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"public_id":"covers/test","secure_url":"https://res.cloudinary.com/testcloud/image/upload/covers/test.jpg"}""",
                Encoding.UTF8, "application/json")
        };
        using var handler = new StaticResponseHandler(response);
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri(address), "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Url.Should().NotBeNull();
        result.PublicId.Should().Be("covers/test");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldReturnFailure_WhenCloudinaryReturnsError()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"error":{"message":"Upload failed"}}""",
                Encoding.UTF8, "application/json")
        };
        using var handler = new StaticResponseHandler(response);
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://covers.openlibrary.org/b/id/123-L.jpg"), "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Upload failed");
        result.Url.Should().BeNull();
        result.PublicId.Should().BeNull();
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldReturnFailure_WhenHttpRequestExceptionThrown()
    {
        // Arrange
        using var handler = new ThrowingHandler(new HttpRequestException("Network error"));
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://covers.openlibrary.org/b/id/123-L.jpg"), "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Network error");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldReturnTimeout_WhenTaskCanceledWithoutUserCancellation()
    {
        // Arrange – throw TaskCanceledException whose token is CancellationToken.None,
        // while the caller token is also None (not cancelled) → triggers the timeout catch branch
        using var handler = new ThrowingHandler(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://covers.openlibrary.org/b/id/123-L.jpg"), "covers", "test", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Upload timeout");
    }

    [Fact]
    public async Task UploadImageFromStreamAsync_Should_ReturnSuccess_WhenUploadSucceeds()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"public_id":"covers/test","secure_url":"https://res.cloudinary.com/testcloud/image/upload/covers/test.jpg"}""",
                Encoding.UTF8, "application/json")
        };
        using var handler = new StaticResponseHandler(response);
        var service = CreateServiceWithMockHandler(handler);
        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]); // minimal JPEG header

        // Act
        var result = await service.UploadImageFromStreamAsync(stream, "cover.jpg", "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Url.Should().NotBeNull();
        result.PublicId.Should().Be("covers/test");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task UploadImageFromStreamAsync_Should_ReturnFailure_WhenUploadFails()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"error":{"message":"Upload failed"}}""",
                Encoding.UTF8, "application/json")
        };
        using var handler = new StaticResponseHandler(response);
        var service = CreateServiceWithMockHandler(handler);
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = await service.UploadImageFromStreamAsync(stream, "cover.webp", "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Upload failed");
        result.Url.Should().BeNull();
        result.PublicId.Should().BeNull();
    }

    [Fact]
    public async Task UploadImageFromStreamAsync_Should_ReturnFailure_WhenHttpError()
    {
        // Arrange
        using var handler = new ThrowingHandler(new HttpRequestException("Network error"));
        var service = CreateServiceWithMockHandler(handler);
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = await service.UploadImageFromStreamAsync(stream, "cover.webp", "covers", "test", TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Network error");
    }

    [Fact]
    public async Task UploadImageFromStreamAsync_Should_ReturnFailure_WhenTimeout()
    {
        // Arrange – TaskCanceledException with CancellationToken.None triggers the timeout branch
        using var handler = new ThrowingHandler(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handler);
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = await service.UploadImageFromStreamAsync(
            stream, "cover.webp", "covers", "test", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Upload timeout");
    }

    [Fact]
    public async Task PingAsync_ShouldReturnTrue_WhenCloudinaryRespondsOk()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"plan":"Free"}""", Encoding.UTF8, "application/json")
        };
        using var handler = new StaticResponseHandler(response);
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.PingAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_ShouldReturnFalse_WhenCloudinaryUnreachable()
    {
        // Arrange
        using var handler = new ThrowingHandler(new HttpRequestException("Network error"));
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.PingAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_Should_ReturnFailure_WhenSchemeIsHttp()
    {
        var settings = Options.Create(new CloudinarySettings
        {
            CloudName = "testcloud",
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret"
        });
        var service = new CloudinaryService(settings, NullLogger<CloudinaryService>.Instance);

        var result = await service.UploadImageFromUrlAsync(
            new Uri("http://covers.openlibrary.org/b/id/1-L.jpg"), "covers", "test",
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not in the allowed list");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_Should_ReturnFailure_WhenHostNotAllowed()
    {
        var settings = Options.Create(new CloudinarySettings
        {
            CloudName = "testcloud",
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret"
        });
        var service = new CloudinaryService(settings, NullLogger<CloudinaryService>.Instance);

        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://example.com/image.jpg"), "covers", "test",
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not in the allowed list");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_Should_CopyAnImageOfTheAccount_AndReduceItTo960Pixels()
    {
        using var handler = new JsonResponsesHandler("""{"public_id":"covers/book","secure_url":"https://res.cloudinary.com/testcloud/image/upload/covers/book.jpg"}""");
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://res.cloudinary.com/testcloud/image/upload/v1/covers/9782205050196.jpg"), "covers", "book",
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        handler.Requests.Should().ContainSingle().Which.Should().Contain("c_limit,w_960");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_Should_ReturnFailure_WhenTheImageIsOfAnotherAccount()
    {
        using var handler = new JsonResponsesHandler();
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://res.cloudinary.com/othercloud/image/upload/v1/covers/1.jpg"), "covers", "book",
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ListImagesAsync_Should_ReadEveryPageOfTheFolder()
    {
        using var handler = new JsonResponsesHandler(
            """{"resources":[{"public_id":"covers/a","bytes":100}],"next_cursor":"next"}""",
            """{"resources":[{"public_id":"covers/b","bytes":200}]}""");
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.ListImagesAsync("covers", TestContext.Current.CancellationToken);

        result.Value.Should().Equal(new CloudinaryImage("covers/a", 100), new CloudinaryImage("covers/b", 200));
        handler.Requests.Should().HaveCount(2);
        handler.Requests[1].Should().Contain("next_cursor=next");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListImagesAsync_Should_ReturnCoversNotListed_WhenCloudinaryFails(bool throws)
    {
        using HttpMessageHandler handler = throws
            ? new ThrowingHandler(new HttpRequestException("Network error"))
            : new JsonResponsesHandler("""{"error":{"message":"Invalid credentials"}}""");
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.ListImagesAsync("covers", TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.CoversNotListed);
    }

    [Fact]
    public async Task DeleteImagesAsync_Should_DeleteTheImagesByBatchesOf100_AndCountTheDeletedOnes()
    {
        var publicIds = Enumerable.Range(0, 101).Select(i => $"covers/{i}").ToList();
        using var handler = new JsonResponsesHandler(
            """{"deleted":{"covers/0":"deleted","covers/1":"not_found"}}""",
            """{"deleted":{"covers/100":"deleted"}}""");
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.DeleteImagesAsync(publicIds, TestContext.Current.CancellationToken);

        result.Value.Should().Be(2);
        handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteImagesAsync_Should_ReturnCoversNotDeleted_WhenCloudinaryFails(bool throws)
    {
        using HttpMessageHandler handler = throws
            ? new ThrowingHandler(new HttpRequestException("Network error"))
            : new JsonResponsesHandler("""{"error":{"message":"Rate limited"}}""");
        var service = CreateServiceWithMockHandler(handler);

        var result = await service.DeleteImagesAsync(["covers/a"], TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.CoversNotDeleted);
    }

    // Answers each request with the next JSON body, and keeps the URL and body of the requests.
    private sealed class JsonResponsesHandler(params string[] bodies) : HttpMessageHandler
    {
        private int _next;

        public List<string> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add($"{request.RequestUri} {content}");
            var body = bodies[Math.Min(_next++, bodies.Length - 1)];
            var status = body.Contains("\"error\"", StringComparison.Ordinal) ? HttpStatusCode.BadRequest : HttpStatusCode.OK;
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw exception;
    }
}
