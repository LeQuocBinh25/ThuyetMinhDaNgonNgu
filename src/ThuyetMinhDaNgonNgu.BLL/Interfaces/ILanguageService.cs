using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageService
{
    IReadOnlyList<SupportedLanguage> GetAll();

    SupportedLanguage? Add(string code, string name);
}
