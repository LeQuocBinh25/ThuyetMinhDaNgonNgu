
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface INarrationService
{
    Task<NarrationResult> CreateNarrationAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<string> CreateDraftAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<NarrationResult> PublishAsync(
        int locationId,
        string languageCode,
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NarrationResult>> GetAllCurrentAsync(CancellationToken cancellationToken = default);

    Task<NarrationResult?> GetCurrentAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);
}
