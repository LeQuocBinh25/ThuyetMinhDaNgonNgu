
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface INarrationService
{
    Task<NarrationResult> CreateNarrationAsync(
        int locationId,
        string languageCode);
}
