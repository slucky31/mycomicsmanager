using Domain.Books;
using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence;


public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Library> Libraries => Set<Library>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<ReadingDate> ReadingDates => Set<ReadingDate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder != null)
        {
            modelBuilder.Entity<Library>().ToTable("Libraries");
            modelBuilder.Entity<Library>().Ignore("RelativePath");

            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<Book>().ToTable("Books");
            modelBuilder.Entity<Book>().Property(b => b.Serie).HasMaxLength(BookConstants.MaxSerieLength);
            modelBuilder.Entity<Book>().Property(b => b.Title).HasMaxLength(BookConstants.MaxTitleLength);
            modelBuilder.Entity<Book>().Property(b => b.ISBN).HasMaxLength(BookConstants.MaxIsbnLength);
            modelBuilder.Entity<Book>().Property(b => b.ImageLink).HasMaxLength(BookConstants.MaxImageLinkLength);
            modelBuilder.Entity<Book>().Property(b => b.Authors).HasMaxLength(BookConstants.MaxAuthorsLength);
            modelBuilder.Entity<Book>().Property(b => b.Publishers).HasMaxLength(BookConstants.MaxPublishersLength);
            modelBuilder.Entity<Book>()
                .HasMany(b => b.ReadingDates)
                .WithOne()
                .HasForeignKey(rd => rd.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReadingDate>().ToTable("ReadingDates");
        }
    }

}
