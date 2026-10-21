using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

public sealed class PostgresNarrationRepository(AppDbContext dbContext) : INarrationRepository
{
    public Task<NarrationRecord?> GetCurrentAsync(int locationId, string languageCode) =>
        dbContext.Narrations.AsNoTracking()
            .Where(item => item.LocationId == locationId && item.LanguageCode == languageCode)
            .Select(ToRecord())
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<NarrationRecord>> GetAllCurrentAsync() =>
        await dbContext.Narrations.AsNoTracking()
            .OrderBy(item => item.Location.Name)
            .ThenBy(item => item.LanguageCode)
            .Select(ToRecord())
            .ToListAsync();

    public async Task<NarrationRecord> SaveCurrentAsync(NarrationRecord narration)
    {
        var entity = new NarrationEntity
        {
            Id = narration.Id,
            LocationId = narration.LocationId,
            LanguageCode = narration.LanguageCode,
            Text = narration.Text,
            AudioObjectPath = narration.AudioObjectPath,
            AudioContentType = narration.AudioContentType,
            Source = narration.Source,
            CreatedAtUtc = narration.CreatedAtUtc,
            UpdatedAtUtc = narration.UpdatedAtUtc
        };

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Narrations"
                ("Id", "LocationId", "LanguageCode", "Text", "AudioObjectPath",
                 "AudioContentType", "Source", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES
                ({entity.Id}, {entity.LocationId}, {entity.LanguageCode}, {entity.Text},
                 {entity.AudioObjectPath}, {entity.AudioContentType}, {entity.Source},
                 {entity.CreatedAtUtc}, {entity.UpdatedAtUtc})
            ON CONFLICT ("LocationId", "LanguageCode") DO UPDATE SET
                "Text" = EXCLUDED."Text",
                "AudioObjectPath" = EXCLUDED."AudioObjectPath",
                "AudioContentType" = EXCLUDED."AudioContentType",
                "Source" = EXCLUDED."Source",
                "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
            """);

        return await GetCurrentAsync(narration.LocationId, narration.LanguageCode)
            ?? throw new InvalidOperationException("Không đọc lại được bản thuyết minh vừa lưu.");
    }

    private static System.Linq.Expressions.Expression<Func<NarrationEntity, NarrationRecord>> ToRecord() =>
        item => new NarrationRecord(
            item.Id,
            item.LocationId,
            item.Location.Name,
            item.LanguageCode,
            item.Language.Name,
            item.Text,
            item.AudioObjectPath,
            item.AudioContentType,
            item.Source,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
}
