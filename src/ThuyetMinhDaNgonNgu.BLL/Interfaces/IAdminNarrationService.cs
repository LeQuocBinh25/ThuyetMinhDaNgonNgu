using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface IAdminNarrationService
{
    Task<IReadOnlyList<NarrationResult>> GetAllAsync();

    Task<NarrationResult?> GetCurrentAsync(int locationId, string languageCode);

    Task<string> CreateDraftAsync(int locationId, string languageCode);

    Task<NarrationResult> PublishAsync(int locationId, string languageCode, string text);
}
