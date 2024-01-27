using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Primitives;

namespace Domain.UnitTests;
public class TErrorTests
{
    // Arrange
    // Act
    // Assert

    [Fact]
    public void Create_Error()
    {
        // Arrange
        string code = "123";
        string description = "Test error";

        // Act
        TError error = new TError(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
    }

    [Fact]
    public void Create_Error_None()
    {
        // Arrange
        // Act
        TError error = TError.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Description.Should().BeNull();
    }
}
