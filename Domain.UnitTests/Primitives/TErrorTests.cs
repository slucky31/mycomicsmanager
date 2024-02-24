using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Primitives;

namespace Domain.UnitTests.Primitives;
public class TErrorTests
{
    // Arrange
    // Act
    // Assert

    [Fact]
    public void Create_Error()
    {
        // Arrange
        var code = "123";
        var description = "Test error";

        // Act
        var error = new TError(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
    }

    [Fact]
    public void Create_Error_None()
    {
        // Arrange
        // Act
        var error = TError.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Description.Should().BeNull();
    }
}
