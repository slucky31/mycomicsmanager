using Application.Interfaces;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence;


public class ApplicationDbContext : DbContext
{
    public DbSet<Library> Libraries { get; set; } 
    
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder !=null)
        {
            modelBuilder.Entity<Library>().ToCollection("libraries");
            modelBuilder.Entity<Library>().Ignore("RelativePath");
        }
    }

}
