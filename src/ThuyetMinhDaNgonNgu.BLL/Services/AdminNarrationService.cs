using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class AdminNarrationService(INarrationService narrations) : IAdminNarrationService
{
    public Task<IReadOnlyList<NarrationResult>> GetAllAsync() =>
        narrations.GetAllCurrentAsync();

    public Task<NarrationResult?> GetCurrentAsync(int locationId, string languageCode) =>
        narrations.GetCurrentAsync(locationId, languageCode);

    public Task<string> CreateDraftAsync(int locationId, string languageCode) =>
        narrations.CreateDraftAsync(locationId, languageCode);

    public Task<NarrationResult> PublishAsync(int locationId, string languageCode, string text) =>
        narrations.PublishAsync(locationId, languageCode, text);
}
