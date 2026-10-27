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

    public Task<IReadOnlyList<SupportedLanguage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<SupportedLanguage?> AddAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        var language = new SupportedLanguage(NormalizeCode(code), NormalizeName(name));

        return AddIfAvailableAsync(language, cancellationToken);
    }

    private async Task<SupportedLanguage?> AddIfAvailableAsync(
        SupportedLanguage language,
        CancellationToken cancellationToken) =>
        await _repository.TryAddAsync(language, cancellationToken) ? language : null;

    public Task<SupportedLanguage?> UpdateAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(
            NormalizeCode(code),
            NormalizeName(name),
            cancellationToken);
    }

    public Task<ResourceDeleteResult> DeleteAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(NormalizeCode(code), cancellationToken);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã ngôn ngữ không được để trống.", nameof(code));

        string normalized = code.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 10 ||
            !normalized.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-'))
        {
            throw new ArgumentException(
                "Mã ngôn ngữ phải có từ 2 đến 10 ký tự và chỉ gồm chữ cái, chữ số hoặc dấu gạch ngang.",
                nameof(code));
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên ngôn ngữ không được để trống.", nameof(name));

        string normalized = name.Trim();
        if (normalized.Length > 100)
            throw new ArgumentException("Tên ngôn ngữ không được vượt quá 100 ký tự.", nameof(name));

        return normalized;
    }
}
