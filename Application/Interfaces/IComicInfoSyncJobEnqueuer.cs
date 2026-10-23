namespace Application.Interfaces;

public interface IComicInfoSyncJobEnqueuer
{
    string Enqueue(Guid userId);
}
