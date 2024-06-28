using Xunit;

namespace Web.Tests;

public class StartupInfoTests
{
    [Fact]
    public void GetInBestUnit_Should_Return_Bytes_When_Size_Less_Than_Mebi()
    {
        // Arrange
        long size = 512;
        string expected = "512 bytes";

        // Act
        string result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInBestUnit_Should_Return_MiB_When_Size_Less_Than_Gibi()
    {
        // Arrange
        long size = 1024 * 1024;
        string expected = "1.00 MiB";

        // Act
        string result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInBestUnit_Should_Return_GiB_When_Size_Greater_Than_Gibi()
    {
        // Arrange
        long size = 1024 * 1024 * 1024;
        string expected = "1.00 GiB";

        // Act
        string result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }
    
}
