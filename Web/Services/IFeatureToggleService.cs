using Domain.Primitives;
using Domain.Settings;
using Web.Models;

namespace Web.Services;

public interface IFeatureToggleService
{
    IReadOnlyList<FeatureToggleViewModel> GetToggles();

    Task<Result> SetAsync(FeatureToggle toggle, bool enabled, CancellationToken cancellationToken = default);
}
