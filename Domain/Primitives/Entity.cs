﻿
using Domain.Interfaces;

namespace Domain.Primitives;
public abstract class Entity<TEntityId> : IAuditable
{    
    protected Entity(TEntityId id)
    {  
        Id = id;
    }

    protected Entity()
    { }

    public TEntityId? Id { get; set; }
    public DateTime CreatedOnUtc { get ; set; }
    public DateTime? ModifiedOnUtc { get; set; }
}