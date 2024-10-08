﻿using Xunit;

namespace Web.Tests;

public class StartupInfoTests
{
    [Fact]
    public void GetInBestUnit_Should_Return_Bytes_When_Size_Less_Than_Mebi()
    {
        // Arrange
        long size = 512;
        var expected = "512 bytes";

        // Act
        var result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInBestUnit_Should_Return_MiB_When_Size_Less_Than_Gibi()
    {
        // Arrange
        long size = 1024 * 1024;
        var expected = "1.00 MiB";

        // Act
        var result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetInBestUnit_Should_Return_GiB_When_Size_Greater_Than_Gibi()
    {
        // Arrange
        long size = 1024 * 1024 * 1024;
        var expected = "1.00 GiB";

        // Act
        var result = StartupInfo.GetInBestUnit(size);

        // Assert
        Assert.Equal(expected, result);
    }

}
