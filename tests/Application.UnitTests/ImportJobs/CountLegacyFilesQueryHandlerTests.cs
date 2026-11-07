using Application.ImportJobs;
using Application.ImportJobs.MigrateLegacy;
using Domain.Errors;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public sealed class CountLegacyFilesQueryHandlerTests
{
    private readonly ILegacyLibrarySource _source = Substitute.For<ILegacyLibrarySource>();

    [Fact]
    public async Task Handle_Should_ReturnTheNumberOfFilesLeftInTheOldLibrary()
    {
        _source.CountFiles().Returns(42);

        var result = await new CountLegacyFilesQueryHandler(_source).Handle(new CountLegacyFilesQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheError_WhenTheOldLibraryFolderDoesNotExist()
    {
        _source.CountFiles().Returns(LegacyMigrationError.SourceNotFound);

        var result = await new CountLegacyFilesQueryHandler(_source).Handle(new CountLegacyFilesQuery(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(LegacyMigrationError.SourceNotFound);
    }
}
