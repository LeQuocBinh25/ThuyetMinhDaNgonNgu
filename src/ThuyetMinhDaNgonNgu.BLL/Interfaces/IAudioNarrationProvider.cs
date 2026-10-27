using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface IAudioNarrationProvider
{
    Task<GeneratedAudio> GenerateAudioAsync(
        string text,
        string languageCode,
        CancellationToken cancellationToken = default);
}