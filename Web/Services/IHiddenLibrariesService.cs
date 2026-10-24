using Domain.Primitives;

namespace Web.Services;

// The setting "Show hidden libraries" of the current user (#1215).
public interface IHiddenLibrariesService
{
    Task<Result<bool>> GetShowHiddenLibraries(CancellationToken cancellationToken = default);

    Task<Result> SetShowHiddenLibraries(bool show, CancellationToken cancellationToken = default);
}
