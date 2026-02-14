using Application.Interfaces;

namespace Application.UnitTests.ComicInfoSearch;

public class CloudinaryUploadResultTests
{
    [Fact]
    public void CloudinaryUploadResult_Should_HaveCorrectProperties_WhenSuccessful()
    {
        // Arrange & Act
        var result = new CloudinaryUploadResult(
            Url: new Uri("https://res.cloudinary.com/test/image/upload/covers/123.jpg"),
            PublicId: "covers/123",
            Success: true,
            Error: null
        );

        // Assert
        result.Success.Should().BeTrue();
        result.Url.Should().NotBeNull();
        result.Url!.ToString().Should().Contain("cloudinary.com");
        result.PublicId.Should().Be("covers/123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void CloudinaryUploadResult_Should_HaveCorrectProperties_WhenFailed()
    {
        // Arrange & Act
        var result = new CloudinaryUploadResult(
            Url: null,
            PublicId: null,
            Success: false,
            Error: "Upload failed"
        );

        // Assert
        result.Success.Should().BeFalse();
        result.Url.Should().BeNull();
        result.PublicId.Should().BeNull();
        result.Error.Should().Be("Upload failed");
    }
}
