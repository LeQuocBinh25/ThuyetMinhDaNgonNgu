using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.SeedData;

/// <summary>Khởi tạo dữ liệu ban đầu để có thể kiểm thử API ngay sau lần chạy đầu tiên.</summary>
public static class DatabaseSeeder
{
    public static void Initialize(AppDbContext dbContext)
    {
        // EnsureCreated tạo schema nếu file SQLite chưa tồn tại.
        // Khi schema ổn định, có thể chuyển sang EF Core Migrations.
        dbContext.Database.EnsureCreated();

        if (!dbContext.Locations.Any())
        {
            dbContext.Locations.AddRange(
                new LocationEntity
                {
                    Id = 1,
                    Name = "Hồ Hoàn Kiếm",
                    City = "Hà Nội",
                    Description = "Hồ nằm ở trung tâm Hà Nội và gắn với truyền thuyết vua Lê trả gươm thần."
                },
                new LocationEntity
                {
                    Id = 2,
                    Name = "Dinh Độc Lập",
                    City = "Thành phố Hồ Chí Minh",
                    Description = "Công trình gắn với nhiều sự kiện quan trọng trong lịch sử Việt Nam, đặc biệt là sự kiện ngày 30 tháng 4 năm 1975."
                },
                new LocationEntity
                {
                    Id = 3,
                    Name = "Vịnh Hạ Long",
                    City = "Quảng Ninh",
                    Description = "Vịnh nổi tiếng với cảnh quan biển và nhiều đảo đá vôi có hình dáng độc đáo."
                },
                new LocationEntity
                {
                    Id = 4,
                    Name = "Chùa Một Cột",
                    City = "Hà Nội",
                    Description = "Chùa Một Cột nổi tiếng nhất nằm ở quận Ba Đình, thành phố Hà Nội"
                });
        }

        if (!dbContext.Languages.Any())
        {
            dbContext.Languages.AddRange(
                new LanguageEntity { Code = "vi", Name = "Tiếng Việt" },
                new LanguageEntity { Code = "en", Name = "English" },
                new LanguageEntity { Code = "ja", Name = "日本語" },
                new LanguageEntity { Code = "ko", Name = "한국어"},
                new LanguageEntity { Code = "fr", Name = "Français"},
                new LanguageEntity { Code = "zh", Name = "中文"});
        }

        dbContext.SaveChanges();
    }
}
