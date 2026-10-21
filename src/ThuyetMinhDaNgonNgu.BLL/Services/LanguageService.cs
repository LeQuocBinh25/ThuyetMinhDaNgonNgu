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

    public Task<IReadOnlyList<SupportedLanguage>> GetAllAsync() =>
        _repository.GetAllAsync();

    public Task<SupportedLanguage?> AddAsync(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã ngôn ngữ không được để trống.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên ngôn ngữ không được để trống.", nameof(name));

        var language = new SupportedLanguage(
            code.Trim().ToLowerInvariant(),
            name.Trim());

        return AddIfAvailableAsync(language);
    }

    private async Task<SupportedLanguage?> AddIfAvailableAsync(SupportedLanguage language) =>
        await _repository.TryAddAsync(language) ? language : null;
}
