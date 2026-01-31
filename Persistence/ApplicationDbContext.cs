using Domain.Books;
using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence;


public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Library>? Libraries { get; set; }

    public DbSet<User>? Users { get; set; }

    public DbSet<Book>? Books { get; set; }

    public DbSet<ReadingDate>? ReadingDates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder != null)
        {
            modelBuilder.Entity<Library>().ToTable("Libraries");
            modelBuilder.Entity<Library>().Ignore("RelativePath");

            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<Book>().ToTable("Books");
            modelBuilder.Entity<Book>()
                .HasMany(b => b.ReadingDates)
                .WithOne()
                .HasForeignKey(rd => rd.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReadingDate>().ToTable("ReadingDates");
        }
    }

}
