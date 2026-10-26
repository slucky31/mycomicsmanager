using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class CoverImageTests
{
    [Theory]
    [InlineData(CoverSize.ExtraSmall, "c_fill,ar_2:3,w_120,f_auto,q_auto")]
    [InlineData(CoverSize.Small, "c_fill,ar_2:3,w_216,f_auto,q_auto")]
    [InlineData(CoverSize.Medium, "c_fill,ar_2:3,w_400,f_auto,q_auto")]
    [InlineData(CoverSize.Large, "c_fill,ar_2:3,w_480,f_auto,q_auto")]
    public void Sized_Should_AskCloudinaryForTheSizeOfTheCover(CoverSize size, string transformation)
    {
        CoverImage.Sized("https://res.cloudinary.com/mcm-ndt/image/upload/v1792919675/covers/2723424189.jpg", size)
            .Should().Be($"https://res.cloudinary.com/mcm-ndt/image/upload/{transformation}/v1792919675/covers/2723424189.jpg");
    }

    [Fact]
    public void Sized_Should_AskCloudinaryForTheSize_WhenTheLinkHasNoVersion()
    {
        CoverImage.Sized("https://res.cloudinary.com/mcm-ndt/image/upload/digital-covers/9782756015996.webp", CoverSize.Small)
            .Should().Be("https://res.cloudinary.com/mcm-ndt/image/upload/c_fill,ar_2:3,w_216,f_auto,q_auto/digital-covers/9782756015996.webp");
    }

    [Theory]
    [InlineData("https://res.cloudinary.com/mcm-ndt/image/upload/c_fill,w_200/v1/covers/a.jpg")]
    [InlineData("https://res.cloudinary.com/mcm-ndt/image/upload/w_200/covers/a.jpg")]
    [InlineData("https://res.cloudinary.com/mcm-ndt/video/upload/v1/a.mp4")]
    [InlineData("https://covers.openlibrary.org/b/id/13161199-L.jpg")]
    [InlineData("https://assets.bubblebd.com/img/zwisd1reza/tj08p3szc3.jpg")]
    [InlineData("/covers/1.webp")]
    [InlineData("")]
    public void Sized_Should_KeepTheLink_WhenCloudinaryCannotResizeIt(string imageLink)
    {
        CoverImage.Sized(imageLink, CoverSize.Medium).Should().Be(imageLink);
    }

    [Fact]
    public void Sized_Should_ReturnAnEmptyLink_WhenThereIsNoCover()
    {
        CoverImage.Sized(null, CoverSize.Medium).Should().BeEmpty();
    }
}
