using Microsoft.EntityFrameworkCore;

namespace KeepSafe.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<WarrantyDocument> WarrantyDocuments => Set<WarrantyDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedAtUtc).IsRequired();

            entity.HasMany(u => u.Warranties)
                  .WithOne(w => w.User)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.ToTable("Warranties");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(w => w.ExpirationDate).HasColumnType("date").IsRequired();
            entity.Property(w => w.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(w => w.FinalPdfSize).IsRequired();
            entity.Property(w => w.CreatedAtUtc).IsRequired();

            entity.HasIndex(w => w.UserId);
            entity.HasIndex(w => w.ExpirationDate);

            entity.HasOne(w => w.Document)
                  .WithOne(d => d.Warranty)
                  .HasForeignKey<WarrantyDocument>(d => d.WarrantyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WarrantyDocument>(entity =>
        {
            entity.ToTable("WarrantyDocuments");
            entity.HasKey(d => d.WarrantyId);
            entity.Property(d => d.PdfData).HasColumnType("longblob").IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(50).IsRequired();
        });
    }
}
