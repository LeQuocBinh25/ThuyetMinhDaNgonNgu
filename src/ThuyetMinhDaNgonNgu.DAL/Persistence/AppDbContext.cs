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
        });

        modelBuilder.Entity<LanguageEntity>(entity =>
        {
            entity.ToTable("Languages");
            entity.HasKey(language => language.Code);
            entity.Property(language => language.Code).HasMaxLength(10);
            entity.Property(language => language.Name).IsRequired().HasMaxLength(100);
        });
    }
}
