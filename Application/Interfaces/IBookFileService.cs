namespace Application.Interfaces;

public interface IBookFileService
{
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
}
