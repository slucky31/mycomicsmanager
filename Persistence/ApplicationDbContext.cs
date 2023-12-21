using Domain.Dto;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence;


public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<LibraryDto> Libraries { get; set; } 
    
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder !=null)
        {
            modelBuilder.Entity<LibraryDto>().ToCollection("libraries");
        }
    }

}
