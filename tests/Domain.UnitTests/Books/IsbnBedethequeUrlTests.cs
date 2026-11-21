using Domain.Books;

namespace Domain.UnitTests.Books;

public class IsbnBedethequeUrlTests
{
    [Fact]
    public void Create_Should_KeepTheIsbnAndTheUrl()
    {
        var link = IsbnBedethequeUrl.Create("9782205050196", "https://www.bedetheque.com/BD-Blacksad-Tome-1-1.html");

        link.Id.Should().NotBe(Guid.Empty);
        link.ISBN.Should().Be("9782205050196");
        link.Url.Should().Be("https://www.bedetheque.com/BD-Blacksad-Tome-1-1.html");
    }
}
