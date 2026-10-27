using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class AdminNarrationService(INarrationService narrations) : IAdminNarrationService
{
    public Task<IReadOnlyList<NarrationResult>> GetAllAsync(CancellationToken cancellationToken = default) =>
        narrations.GetAllCurrentAsync(cancellationToken);

    public Task<NarrationResult?> GetCurrentAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default) =>
        narrations.GetCurrentAsync(locationId, languageCode, cancellationToken);

    public Task<string> CreateDraftAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default) =>
        narrations.CreateDraftAsync(locationId, languageCode, cancellationToken);

    public Task<NarrationResult> PublishAsync(
        int locationId,
        string languageCode,
        string text,
        CancellationToken cancellationToken = default) =>
        narrations.PublishAsync(locationId, languageCode, text, cancellationToken);

    public Task DeleteAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default) =>
        narrations.DeleteAsync(locationId, languageCode, cancellationToken);
}
