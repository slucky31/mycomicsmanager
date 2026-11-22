using Base.Integration.Tests;
using Domain.FeedImports;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public class HostStatusChangeRepositoryTests(IntegrationTestWebAppFactory factory) : HostStatusChangeIntegrationTest(factory)
{
    private static readonly DateTime s_now = new(2026, 11, 21, 23, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    // Unique names: the tests share the database (each one in its own transaction).
    private static string NewHost() => $"host-{Guid.CreateVersion7():N}";

    private async Task<HostStatusChange> AddAsync(string host, bool isUp, DateTime at)
    {
        var change = HostStatusChange.Create(host, isUp, new FixedTimeProvider(at)).Value!;
        HostStatusChangeRepository.Add(change);
        await UnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return change;
    }

    [Fact]
    public async Task GetLatestPerHostAsync_Should_ReturnTheNewestChangeOfEachHost()
    {
        var ddl = NewHost();
        var fichier = NewHost();
        await AddAsync(ddl, true, s_now.AddDays(-2));
        var ddlDown = await AddAsync(ddl, false, s_now.AddHours(-1));
        var fichierUp = await AddAsync(fichier, true, s_now.AddDays(-5));
        Context.ChangeTracker.Clear();

        var latest = await HostStatusChangeRepository.GetLatestPerHostAsync(TestContext.Current.CancellationToken);

        latest[ddl].Id.Should().Be(ddlDown.Id);
        latest[ddl].IsUp.Should().BeFalse();
        latest[fichier].Id.Should().Be(fichierUp.Id);
    }

    [Fact]
    public async Task ListSinceAsync_Should_ReturnTheRecentChanges_NewestFirst()
    {
        var host = NewHost();
        await AddAsync(host, true, s_now.AddDays(-100));
        var up = await AddAsync(host, true, s_now.AddDays(-3));
        var down = await AddAsync(host, false, s_now.AddHours(-1));

        var changes = await HostStatusChangeRepository.ListSinceAsync(s_now.AddDays(-90), TestContext.Current.CancellationToken);

        changes.Where(c => c.Host == host).Select(c => c.Id).Should().Equal(down.Id, up.Id);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_Should_KeepTheLatestChangeOfEachHost()
    {
        var changing = NewHost();
        var stable = NewHost();
        await AddAsync(changing, true, s_now.AddDays(-120));
        var recent = await AddAsync(changing, false, s_now.AddDays(-1));
        var onlyOne = await AddAsync(stable, true, s_now.AddDays(-200));

        var deleted = await HostStatusChangeRepository.DeleteOlderThanAsync(s_now.AddDays(-90), TestContext.Current.CancellationToken);

        deleted.Should().BeGreaterThanOrEqualTo(1);
        Context.ChangeTracker.Clear();
        var left = Context.HostStatusChanges.Where(c => c.Host == changing || c.Host == stable).Select(c => c.Id).ToList();
        left.Should().BeEquivalentTo([recent.Id, onlyOne.Id]);
    }
}
