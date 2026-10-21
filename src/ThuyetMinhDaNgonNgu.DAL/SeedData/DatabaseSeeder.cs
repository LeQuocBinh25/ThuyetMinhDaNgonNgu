using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.SeedData;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(AppDbContext dbContext)
    {
        if (!await dbContext.Locations.AnyAsync())
        {
            dbContext.Locations.AddRange(
                new LocationEntity
                {
                    Id = 1,
                    Name = "Hồ Hoàn Kiếm",
                    City = "Hà Nội",
                    Description = "Hồ nằm ở trung tâm Hà Nội và gắn với truyền thuyết vua Lê trả gươm thần.",
                    Latitude = 21.0287,
                    Longitude = 105.8524
                },
                new LocationEntity
                {
                    Id = 2,
                    Name = "Dinh Độc Lập",
                    City = "Thành phố Hồ Chí Minh",
                    Description = "Công trình gắn với nhiều sự kiện quan trọng trong lịch sử Việt Nam, đặc biệt là sự kiện ngày 30 tháng 4 năm 1975.",
                    Latitude = 10.7770,
                    Longitude = 106.6953
                },
                new LocationEntity
                {
                    Id = 3,
                    Name = "Vịnh Hạ Long",
                    City = "Quảng Ninh",
                    Description = "Vịnh nổi tiếng với cảnh quan biển và nhiều đảo đá vôi có hình dáng độc đáo.",
                    Latitude = 20.9101,
                    Longitude = 107.1839
                },
                new LocationEntity
                {
                    Id = 4,
                    Name = "Chùa Một Cột",
                    City = "Hà Nội",
                    Description = "Chùa Một Cột nổi tiếng nhất nằm ở quận Ba Đình, thành phố Hà Nội.",
                    Latitude = 21.0359,
                    Longitude = 105.8342
                });
        }

        await AddKnownCoordinateIfMissingAsync(dbContext, "Hồ Hoàn Kiếm", 21.0287, 105.8524);
        await AddKnownCoordinateIfMissingAsync(dbContext, "Dinh Độc Lập", 10.7770, 106.6953);
        await AddKnownCoordinateIfMissingAsync(dbContext, "Vịnh Hạ Long", 20.9101, 107.1839);
        await AddKnownCoordinateIfMissingAsync(dbContext, "Chùa Một Cột", 21.0359, 105.8342);

        if (!await dbContext.Languages.AnyAsync())
        {
            dbContext.Languages.AddRange(
                new LanguageEntity { Code = "vi", Name = "Tiếng Việt" },
                new LanguageEntity { Code = "en", Name = "English" },
                new LanguageEntity { Code = "ja", Name = "日本語" },
                new LanguageEntity { Code = "ko", Name = "한국어" },
                new LanguageEntity { Code = "fr", Name = "Français" },
                new LanguageEntity { Code = "zh", Name = "中文" });
        }

        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync("""
            SELECT setval(
                pg_get_serial_sequence('"Locations"', 'Id'),
                COALESCE(MAX("Id"), 1),
                MAX("Id") IS NOT NULL)
            FROM "Locations";
            """);
    }

    private static async Task AddKnownCoordinateIfMissingAsync(
        AppDbContext dbContext,
        string name,
        double latitude,
        double longitude)
    {
        var location = await dbContext.Locations
            .FirstOrDefaultAsync(item => item.Name == name);
        if (location is not null && (!location.Latitude.HasValue || !location.Longitude.HasValue))
        {
            location.Latitude = latitude;
            location.Longitude = longitude;
        }
    }
}
