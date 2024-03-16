using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence;


public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Library> Libraries { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder !=null)
        {
            modelBuilder.Entity<Library>().ToCollection("libraries");
            modelBuilder.Entity<Library>().Ignore("RelativePath");

            modelBuilder.Entity<User>().ToCollection("users");
        }
    }

}
