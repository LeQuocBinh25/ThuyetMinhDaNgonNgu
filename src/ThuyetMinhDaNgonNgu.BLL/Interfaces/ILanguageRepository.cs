using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageRepository
{
    Task<IReadOnlyList<SupportedLanguage>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SupportedLanguage?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(SupportedLanguage language, CancellationToken cancellationToken = default);

    Task<SupportedLanguage?> UpdateAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default);

    Task<ResourceDeleteResult> DeleteAsync(string code, CancellationToken cancellationToken = default);
}
