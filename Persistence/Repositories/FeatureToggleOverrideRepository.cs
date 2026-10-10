using Application.Interfaces;
using Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class FeatureToggleOverrideRepository(ApplicationDbContext context) : IFeatureToggleOverrideRepository
{
    public void Add(FeatureToggleOverride toggleOverride) => context.FeatureToggleOverrides.Add(toggleOverride);

    public void Remove(FeatureToggleOverride toggleOverride) => context.FeatureToggleOverrides.Remove(toggleOverride);

    public async Task<FeatureToggleOverride?> GetAsync(FeatureToggle toggle, CancellationToken ct = default)
        => await context.FeatureToggleOverrides.FirstOrDefaultAsync(o => o.Toggle == toggle, ct);

    public async Task<IReadOnlyList<FeatureToggleOverride>> GetAllAsync(CancellationToken ct = default)
        => await context.FeatureToggleOverrides.AsNoTracking().ToListAsync(ct);
}
