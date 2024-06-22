using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
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

    [SkippableFact]
    public void GetBestValue_ReturnsTrue_WhenValidPathExists()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        // Arrange
        string[] paths = ["/sys/fs/cgroup/memory.max"];

        // Act
        bool result = StartupInfo.GetBestValue(paths, out var limit, out var bestPath);

        // Assert
        Assert.True(result);
        Assert.Equal("/sys/fs/cgroup/memory.max", bestPath);
        Assert.NotEqual(0, limit);
    }

    [Fact]
    public void GetBestValue_ReturnsFalse_WhenInvalidPathExists()
    {
        // Arrange
        string[] paths = ["/sys/fs/cgroup/memory.invalid"];

        // Act
        bool result = StartupInfo.GetBestValue(paths, out var limit, out var bestPath);

        // Assert
        Assert.False(result);
        Assert.Null(bestPath);
        Assert.Equal(0, limit);
    }

    [Fact]
    public void GetBestValue_ReturnsFalse_WhenPathDoesNotExist()
    {
        // Arrange
        string[] paths = ["/sys/fs/cgroup/memory.nonexistent"];

        // Act
        bool result = StartupInfo.GetBestValue(paths, out var limit, out var bestPath);

        // Assert
        Assert.False(result);
        Assert.Null(bestPath);
        Assert.Equal(0, limit);
    }
}
