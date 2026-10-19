using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageRepository
{
    IReadOnlyList<SupportedLanguage> GetAll();

    SupportedLanguage? GetByCode(string code);

    bool TryAdd(SupportedLanguage language);
}
