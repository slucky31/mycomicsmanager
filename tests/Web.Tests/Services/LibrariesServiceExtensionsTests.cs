using AwesomeAssertions;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class LibrariesServiceExtensionsTests
{
    private readonly ILibrariesService _librariesService = Substitute.For<ILibrariesService>();

    [Fact]
    public async Task NameOfAsync_Should_ReturnTheName_WhenTheLibraryIsFound()
    {
        var library = Library.Create("Read Books", "#2E7D32", "Star", LibraryBookType.Physical, Guid.CreateVersion7()).Value!;
        _librariesService.GetById(library.Id.ToString()).Returns(_ => Result<Library>.Success(library));

        var name = await _librariesService.NameOfAsync(library.Id);

        name.Should().Be("Read Books");
    }

    [Fact]
    public async Task NameOfAsync_Should_ReturnNull_WhenTheLibraryCannotBeRead()
    {
        _librariesService.GetById(Arg.Any<string?>()).Returns(_ => Result<Library>.Failure(LibrariesError.NotFound));

        var name = await _librariesService.NameOfAsync(Guid.CreateVersion7());

        name.Should().BeNull();
    }

    [Fact]
    public async Task NameOfAsync_Should_ReturnNull_WhenNoLibraryIsGiven()
    {
        var name = await _librariesService.NameOfAsync(null);

        name.Should().BeNull();
        await _librariesService.DidNotReceive().GetById(Arg.Any<string?>());
    }
}
