using Application;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Persistence.Repositories;
public class LibraryRepository : Repository<Library, string>
{
    public LibraryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
