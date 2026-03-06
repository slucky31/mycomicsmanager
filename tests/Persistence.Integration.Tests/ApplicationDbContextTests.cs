using Base.Integration.Tests;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Tests;

[Collection("DatabaseCollectionTests")]
public class ApplicationDbContextTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public void Libraries_ShouldReturnDbSet()
    {
        Context.Libraries.Should().NotBeNull();
        Context.Libraries.Should().BeAssignableTo<DbSet<Domain.Libraries.Library>>();
    }

    [Fact]
    public void Users_ShouldReturnDbSet()
    {
        Context.Users.Should().NotBeNull();
        Context.Users.Should().BeAssignableTo<DbSet<Domain.Users.User>>();
    }

    [Fact]
    public void Books_ShouldReturnDbSet()
    {
        Context.Books.Should().NotBeNull();
        Context.Books.Should().BeAssignableTo<DbSet<Domain.Books.Book>>();
    }

    [Fact]
    public void PhysicalBooks_ShouldReturnDbSet()
    {
        Context.PhysicalBooks.Should().NotBeNull();
        Context.PhysicalBooks.Should().BeAssignableTo<DbSet<Domain.Books.PhysicalBook>>();
    }

    [Fact]
    public void DigitalBooks_ShouldReturnDbSet()
    {
        Context.DigitalBooks.Should().NotBeNull();
        Context.DigitalBooks.Should().BeAssignableTo<DbSet<Domain.Books.DigitalBook>>();
    }

    [Fact]
    public void ReadingDates_ShouldReturnDbSet()
    {
        Context.ReadingDates.Should().NotBeNull();
        Context.ReadingDates.Should().BeAssignableTo<DbSet<Domain.Books.ReadingDate>>();
    }
}
