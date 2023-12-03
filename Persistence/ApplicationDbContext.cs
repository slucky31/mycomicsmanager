using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence;


public class ApplicationDbContext : DbContext
{
    public DbSet<Library> Libraries { get; set; } 
    
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public static ApplicationDbContext? Create(IMongoDatabase database)
    {
        if (database == null)
        {
            return null;
        }

        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder !=null)
        {
            modelBuilder.Entity<Library>().ToCollection("libraries");
        }        
    }

}
