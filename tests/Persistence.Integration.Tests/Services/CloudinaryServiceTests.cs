using System.Net;
using System.Reflection;
using System.Text;
using Application.ComicInfoSearch;
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
            ApiKey = "123456789012345",
            ApiSecret = "abcdefsecretkey"
        });

        var service = new CloudinaryService(settings);

        // Inject mock HttpClient via reflection: service._cloudinary.Api.Client
        var cloudinaryField = typeof(CloudinaryService)
            .GetField("_cloudinary", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cloudinary = cloudinaryField.GetValue(service)!;
        var api = cloudinary.GetType().GetProperty("Api")!.GetValue(cloudinary)!;
        api.GetType().GetField("Client")!.SetValue(api, new HttpClient(handler));

        return service;
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldReturnSuccess_WhenUploadSucceeds()
    {
        // Arrange
        var handler = new StaticResponseHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"public_id":"covers/test","secure_url":"https://res.cloudinary.com/testcloud/image/upload/covers/test.jpg"}""",
                Encoding.UTF8, "application/json")
        });
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://example.com/image.jpg"), "covers", "test");

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
        var handler = new StaticResponseHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"error":{"message":"Upload failed"}}""",
                Encoding.UTF8, "application/json")
        });
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://example.com/image.jpg"), "covers", "test");

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
        var handler = new ThrowingHandler(new HttpRequestException("Network error"));
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://example.com/image.jpg"), "covers", "test");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Network error");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldReturnTimeout_WhenTaskCanceledWithoutUserCancellation()
    {
        // Arrange – throw TaskCanceledException whose token is CancellationToken.None,
        // while the caller token is also None (not cancelled) → triggers the timeout catch branch
        var handler = new ThrowingHandler(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handler);

        // Act
        var result = await service.UploadImageFromUrlAsync(
            new Uri("https://example.com/image.jpg"), "covers", "test", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Upload timeout");
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
