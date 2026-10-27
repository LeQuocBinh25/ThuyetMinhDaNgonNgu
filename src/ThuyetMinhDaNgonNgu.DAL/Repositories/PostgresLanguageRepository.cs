using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

public sealed class PostgresLanguageRepository(AppDbContext dbContext) : ILanguageRepository
{
    public async Task<IReadOnlyList<SupportedLanguage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Languages.AsNoTracking()
            .OrderBy(language => language.Code)
            .Select(language => new SupportedLanguage(language.Code, language.Name))
            .ToListAsync(cancellationToken);

    public async Task<SupportedLanguage?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        await dbContext.Languages.AsNoTracking()
            .Where(language => language.Code == code.Trim().ToLowerInvariant())
            .Select(language => new SupportedLanguage(language.Code, language.Name))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryAddAsync(
        SupportedLanguage language,
        CancellationToken cancellationToken = default)
    {
        dbContext.Languages.Add(new LanguageEntity
        {
            Code = language.Code,
            Name = language.Name
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
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

    public async Task<SupportedLanguage?> UpdateAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Languages.FirstOrDefaultAsync(
            language => language.Code == code,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SupportedLanguage(entity.Code, entity.Name);
    }

    public async Task<ResourceDeleteResult> DeleteAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int deleted = await dbContext.Languages
                .Where(language => language.Code == code &&
                    !dbContext.Narrations.Any(narration => narration.LanguageCode == code))
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                return ResourceDeleteResult.Deleted;
            }

            return await dbContext.Languages.AnyAsync(
                language => language.Code == code,
                cancellationToken)
                ? ResourceDeleteResult.InUse
                : ResourceDeleteResult.NotFound;
        }
        catch (PostgresException exception) when
            (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return ResourceDeleteResult.InUse;
        }
    }
}
