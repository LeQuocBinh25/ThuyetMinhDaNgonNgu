using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.DAL.Entities;

namespace ThuyetMinhDaNgonNgu.DAL.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<LocationEntity> Locations => Set<LocationEntity>();

    public DbSet<LanguageEntity> Languages => Set<LanguageEntity>();

    public DbSet<NarrationEntity> Narrations => Set<NarrationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocationEntity>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Name).IsRequired().HasMaxLength(200);
            entity.Property(location => location.City).IsRequired().HasMaxLength(200);
            entity.Property(location => location.Description).IsRequired().HasMaxLength(4000);
            entity.Property(location => location.Latitude).HasPrecision(9, 6);
            entity.Property(location => location.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<LanguageEntity>(entity =>
        {
            entity.ToTable("Languages");
            entity.HasKey(language => language.Code);
            entity.Property(language => language.Code).HasMaxLength(10);
            entity.Property(language => language.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<NarrationEntity>(entity =>
        {
            entity.ToTable("Narrations");
            entity.HasKey(narration => narration.Id);
            entity.Property(narration => narration.Text).IsRequired().HasMaxLength(50000);
            entity.Property(narration => narration.AudioObjectPath).IsRequired().HasMaxLength(500);
            entity.Property(narration => narration.AudioContentType).IsRequired().HasMaxLength(100);
            entity.Property(narration => narration.Source).IsRequired().HasMaxLength(30);
            entity.HasIndex(narration => new { narration.LocationId, narration.LanguageCode })
                .IsUnique();
            entity.HasOne(narration => narration.Location)
                .WithMany()
                .HasForeignKey(narration => narration.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(narration => narration.Language)
                .WithMany()
                .HasForeignKey(narration => narration.LanguageCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
