using Domain.Settings;

namespace Application.Interfaces;

public interface IFeatureToggleOverrideRepository
{
    void Add(FeatureToggleOverride toggleOverride);
    void Remove(FeatureToggleOverride toggleOverride);
    Task<FeatureToggleOverride?> GetAsync(FeatureToggle toggle, CancellationToken ct = default);
    Task<IReadOnlyList<FeatureToggleOverride>> GetAllAsync(CancellationToken ct = default);
}
