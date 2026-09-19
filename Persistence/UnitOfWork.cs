using Application.Interfaces;
using Domain.Interfaces;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        UpdateAuditableEntities();
        try
        {
            var changesNb = await dbContext.SaveChangesAsync(cancellationToken);
            return changesNb;
        }
        catch (DbUpdateException ex)
        {
            return new TError("DB_UPDATE_ERROR", ex.Message);
        }
    }

    private void UpdateAuditableEntities()
    {
        var entries = dbContext.ChangeTracker.Entries<IAuditable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(p => p.CreatedOnUtc).CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(p => p.ModifiedOnUtc).CurrentValue = DateTime.UtcNow;
            }
        }
    }


}
