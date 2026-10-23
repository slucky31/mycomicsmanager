namespace Application.Interfaces;

public interface ILegacyMigrationJobEnqueuer
{
    string Enqueue(int count, Guid userId);
}
