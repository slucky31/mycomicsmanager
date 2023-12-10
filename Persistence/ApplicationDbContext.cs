using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence;


public class ApplicationDbContext : DbContext, IApplicationDbContext
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
        }

    }

}
