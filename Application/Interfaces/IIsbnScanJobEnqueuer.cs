namespace Application.Interfaces;

public interface IIsbnScanJobEnqueuer
{
    string Enqueue(Guid libraryId);
}
