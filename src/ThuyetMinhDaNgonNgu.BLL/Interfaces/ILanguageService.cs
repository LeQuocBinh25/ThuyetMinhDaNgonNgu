using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageService
{
    Task<IReadOnlyList<SupportedLanguage>> GetAllAsync();

    Task<SupportedLanguage?> AddAsync(string code, string name);
}
