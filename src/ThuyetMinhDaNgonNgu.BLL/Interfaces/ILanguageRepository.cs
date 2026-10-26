using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageRepository
{
    Task<IReadOnlyList<SupportedLanguage>> GetAllAsync();

    Task<SupportedLanguage?> GetByCodeAsync(string code);

    Task<bool> TryAddAsync(SupportedLanguage language);
}
