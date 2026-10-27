using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface INarrationRepository
{
    Task<NarrationRecord?> GetCurrentAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NarrationRecord>> GetAllCurrentAsync(CancellationToken cancellationToken = default);

    Task<NarrationRecord> SaveCurrentAsync(
        NarrationRecord narration,
        CancellationToken cancellationToken = default);

    Task<NarrationRecord?> DeleteCurrentAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<bool> IsAudioObjectReferencedAsync(
        string objectPath,
        CancellationToken cancellationToken = default);
}
