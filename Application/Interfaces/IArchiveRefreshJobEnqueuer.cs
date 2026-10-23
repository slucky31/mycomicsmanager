namespace Application.Interfaces;

public interface IArchiveRefreshJobEnqueuer
{
    string Enqueue(Guid userId);
}
