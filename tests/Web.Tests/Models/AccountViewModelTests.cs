using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class AccountViewModelTests
{
    [Theory]
    [InlineData("Nicolas DUFAUT", "n@example.com", "ND")]
    [InlineData("  jean  pierre  martin ", null, "JM")]
    [InlineData("alice", null, "A")]
    [InlineData("", "bob@example.com", "B")]
    [InlineData(null, " ", "?")]
    public void Initials_Should_UseNameThenEmail(string? name, string? email, string expected)
    {
        AccountViewModel.Initials(name, email).Should().Be(expected);
    }
}
