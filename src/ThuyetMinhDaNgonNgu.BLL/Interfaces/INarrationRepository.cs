using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface INarrationRepository
{
    Task<NarrationRecord?> GetCurrentAsync(int locationId, string languageCode);

    Task<IReadOnlyList<NarrationRecord>> GetAllCurrentAsync();

    Task<NarrationRecord> SaveCurrentAsync(NarrationRecord narration);
}
