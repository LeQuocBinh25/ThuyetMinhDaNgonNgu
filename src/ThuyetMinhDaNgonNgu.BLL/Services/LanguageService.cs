using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _repository;

    public LanguageService(ILanguageRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<SupportedLanguage> GetAll() => _repository.GetAll();

    public SupportedLanguage? Add(string code, string name)
    {
        var language = new SupportedLanguage(
            code.Trim().ToLowerInvariant(),
            name.Trim());

        return _repository.TryAdd(language) ? language : null;
    }
}
