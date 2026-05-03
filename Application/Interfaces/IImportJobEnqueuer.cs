namespace Application.Interfaces;

public interface IImportJobEnqueuer
{
    string Enqueue(Guid importJobId);
}
