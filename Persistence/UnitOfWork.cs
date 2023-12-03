using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Domain.Primitives;

namespace Persistence;
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        IEnumerable<EntityEntry<IAuditable>> entries =
            _dbContext.ChangeTracker
            .Entries<IAuditable>();

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
