using Application.Interfaces;
using Domain.Interfaces;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class UnitOfWork(ApplicationDbContext dbContext, TimeProvider timeProvider) : IUnitOfWork
{
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        UpdateAuditableEntities();
        try
        {
            var changesNb = await dbContext.SaveChangesAsync(cancellationToken);
            return changesNb;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The tracked entities are stale: keeping them would make every later save of this context fail too.
            dbContext.ChangeTracker.Clear();
            return PersistenceError.ConcurrencyConflict;
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
                entry.Property(p => p.CreatedOnUtc).CurrentValue = timeProvider.GetUtcNow().UtcDateTime;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(p => p.ModifiedOnUtc).CurrentValue = timeProvider.GetUtcNow().UtcDateTime;
            }
        }
    }


}
