using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Application;

public interface IApplicationDbContext
{
    DbSet<Library> Libraries { get; set; }
    // TODO : Move to Persistence
}
