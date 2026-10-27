using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILanguageService
{
    Task<IReadOnlyList<SupportedLanguage>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SupportedLanguage?> AddAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default);

    Task<SupportedLanguage?> UpdateAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default);

    Task<ResourceDeleteResult> DeleteAsync(string code, CancellationToken cancellationToken = default);
}
