namespace Application.Interfaces;

public interface ILegacyMigrationOrchestrator
{
    Task MigrateAsync(int count, Guid userId, CancellationToken ct = default);
}
