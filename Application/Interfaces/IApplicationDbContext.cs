using Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Library> Libraries { get; set; }
}
