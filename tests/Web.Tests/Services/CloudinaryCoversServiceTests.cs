using Application.Abstractions.Messaging;
using Application.Books.Covers;
using AwesomeAssertions;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class CloudinaryCoversServiceTests
{
    private readonly IQueryHandler<CountCoversToMoveQuery, int> _count = Substitute.For<IQueryHandler<CountCoversToMoveQuery, int>>();
    private readonly ICommandHandler<StartCoverMoveCommand, int> _start = Substitute.For<ICommandHandler<StartCoverMoveCommand, int>>();
    private readonly IQueryHandler<FindUnusedCoversQuery, UnusedCovers> _find = Substitute.For<IQueryHandler<FindUnusedCoversQuery, UnusedCovers>>();
    private readonly ICommandHandler<DeleteUnusedCoversCommand, DeletedCovers> _delete = Substitute.For<ICommandHandler<DeleteUnusedCoversCommand, DeletedCovers>>();
    private readonly CloudinaryCoversService _service;

    public CloudinaryCoversServiceTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _count);
        services.AddScoped(_ => _start);
        services.AddScoped(_ => _find);
        services.AddScoped(_ => _delete);
        _service = new CloudinaryCoversService(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public async Task Service_Should_PassEachRequestToItsHandler()
    {
        var unused = new UnusedCovers(["covers/a"], 100);
        _count.Handle(Arg.Any<CountCoversToMoveQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(4));
        _start.Handle(Arg.Any<StartCoverMoveCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(5));
        _find.Handle(Arg.Any<FindUnusedCoversQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(unused));
        _delete.Handle(Arg.Is<DeleteUnusedCoversCommand>(c => c.PublicIds.SequenceEqual(unused.PublicIds)), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new DeletedCovers(1, 100)));
        var ct = TestContext.Current.CancellationToken;

        (await _service.CountToMoveAsync(ct)).Value.Should().Be(4);
        (await _service.StartMoveAsync(ct)).Value.Should().Be(5);
        (await _service.FindUnusedAsync(ct)).Value.Should().Be(unused);
        (await _service.DeleteUnusedAsync(unused.PublicIds, ct)).Value.Should().Be(new DeletedCovers(1, 100));
    }
}
