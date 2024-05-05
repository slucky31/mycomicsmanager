using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;
public interface ILibrariesService
{
    Task<Result<Library>> Create(string? name);
    Task<Result<Library>> GetById(string? id);
    Task<Result<Library>> Update(string? id, string? name);
}
