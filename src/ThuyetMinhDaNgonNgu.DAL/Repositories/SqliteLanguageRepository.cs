using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

/// <summary>Truy cập ngôn ngữ được lưu trong SQLite.</summary>
public sealed class SqliteLanguageRepository : ILanguageRepository
{
    private readonly AppDbContext _dbContext;

    public SqliteLanguageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<SupportedLanguage> GetAll() =>
        _dbContext.Languages
            .AsNoTracking()
            .OrderBy(language => language.Code)
            .ToList()
            .Select(language => new SupportedLanguage(language.Code, language.Name))
            .ToArray();

    public SupportedLanguage? GetByCode(string code)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        var language = _dbContext.Languages
            .AsNoTracking()
            .FirstOrDefault(item => item.Code == normalizedCode);

        return language is null ? null : new SupportedLanguage(language.Code, language.Name);
    }

    public bool TryAdd(SupportedLanguage language)
    {
        if (_dbContext.Languages.Any(item => item.Code == language.Code))
        {
            return false;
        }

        _dbContext.Languages.Add(new Entities.LanguageEntity
        {
            Code = language.Code,
            Name = language.Name
        });
        _dbContext.SaveChanges();
        return true;
    }
}
