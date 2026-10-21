
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface INarrationService
{
    Task<NarrationResult> CreateNarrationAsync(
        int locationId,
        string languageCode);

    Task<string> CreateDraftAsync(int locationId, string languageCode);

    Task<NarrationResult> PublishAsync(
        int locationId,
        string languageCode,
        string text);

    Task<IReadOnlyList<NarrationResult>> GetAllCurrentAsync();

    Task<NarrationResult?> GetCurrentAsync(int locationId, string languageCode);
}
