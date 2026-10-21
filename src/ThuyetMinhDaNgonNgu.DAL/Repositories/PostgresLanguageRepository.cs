using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

public sealed class PostgresLanguageRepository(AppDbContext dbContext) : ILanguageRepository
{
    public async Task<IReadOnlyList<SupportedLanguage>> GetAllAsync() =>
        await dbContext.Languages.AsNoTracking()
            .OrderBy(language => language.Code)
            .Select(language => new SupportedLanguage(language.Code, language.Name))
            .ToListAsync();

    public async Task<SupportedLanguage?> GetByCodeAsync(string code) =>
        await dbContext.Languages.AsNoTracking()
            .Where(language => language.Code == code.Trim().ToLowerInvariant())
            .Select(language => new SupportedLanguage(language.Code, language.Name))
            .FirstOrDefaultAsync();

    public async Task<bool> TryAddAsync(SupportedLanguage language)
    {
        dbContext.Languages.Add(new LanguageEntity
        {
            Code = language.Code,
            Name = language.Name
        });

        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(dbContext.Languages.Local.Single(item => item.Code == language.Code)).State =
                EntityState.Detached;
            return false;
        }
    }
}
