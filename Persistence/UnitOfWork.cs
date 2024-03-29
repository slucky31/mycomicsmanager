﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Application.Data;
using Domain.Interfaces;
using Domain.Primitives;

namespace Persistence;
public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        UpdateAuditableEntities();
        int changesNb = await dbContext.SaveChangesAsync(cancellationToken);
        return changesNb;
    }

    private void UpdateAuditableEntities()
    {
        IEnumerable<EntityEntry<IAuditable>> entries =
            dbContext.ChangeTracker
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
