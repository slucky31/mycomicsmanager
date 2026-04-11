using Domain.Books;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence;


public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Library> Libraries => Set<Library>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<PhysicalBook> PhysicalBooks => Set<PhysicalBook>();

    public DbSet<DigitalBook> DigitalBooks => Set<DigitalBook>();

    public DbSet<ReadingDate> ReadingDates => Set<ReadingDate>();

    public DbSet<IsbnBedethequeUrl> IsbnBedethequeUrls => Set<IsbnBedethequeUrl>();

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (modelBuilder != null)
        {
            modelBuilder.Entity<Library>().ToTable("Libraries");
            modelBuilder.Entity<Library>().Ignore("RelativePath");
            modelBuilder.Entity<Library>().Property(l => l.Name).HasMaxLength(LibraryConstants.MaxNameLength);
            modelBuilder.Entity<Library>().Property(l => l.Color).HasMaxLength(LibraryConstants.MaxColorLength);
            modelBuilder.Entity<Library>().Property(l => l.Icon).HasMaxLength(LibraryConstants.MaxIconLength);

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

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Library)
                .WithMany(l => l.Books)
                .HasForeignKey(b => b.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);

            // TPT (Table Per Type) for Book hierarchy
            modelBuilder.Entity<PhysicalBook>().ToTable("PhysicalBooks");
            modelBuilder.Entity<DigitalBook>().ToTable("DigitalBooks");
            modelBuilder.Entity<DigitalBook>().Property(b => b.FilePath).HasMaxLength(BookConstants.MaxFilePathLength);

            modelBuilder.Entity<ImportJob>().ToTable("ImportJobs");
            modelBuilder.Entity<ImportJob>().Property(j => j.OriginalFileName).HasMaxLength(ImportJobConstants.MaxFileNameLength);
            modelBuilder.Entity<ImportJob>().Property(j => j.OriginalFilePath).HasMaxLength(ImportJobConstants.MaxFilePathLength);
            modelBuilder.Entity<ImportJob>().Property(j => j.ErrorMessage).HasMaxLength(ImportJobConstants.MaxErrorMessageLength);
            modelBuilder.Entity<ImportJob>().Property(j => j.ErrorStep).HasMaxLength(ImportJobConstants.MaxErrorStepLength);
            modelBuilder.Entity<ImportJob>().HasIndex(j => j.LibraryId);
            modelBuilder.Entity<ImportJob>().HasIndex(j => j.Status);

            modelBuilder.Entity<ReadingDate>().ToTable("ReadingDates");

            modelBuilder.Entity<IsbnBedethequeUrl>().ToTable("IsbnBedethequeUrls");
            modelBuilder.Entity<IsbnBedethequeUrl>().Property(x => x.ISBN).HasMaxLength(20);
            modelBuilder.Entity<IsbnBedethequeUrl>().Property(x => x.Url).HasMaxLength(500);
            modelBuilder.Entity<IsbnBedethequeUrl>().HasIndex(x => x.ISBN).IsUnique();
        }
    }
}
