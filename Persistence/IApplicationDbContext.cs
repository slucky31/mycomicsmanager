using Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public interface IApplicationDbContext
{
    DbSet<Library> Libraries { get; set; }
}
