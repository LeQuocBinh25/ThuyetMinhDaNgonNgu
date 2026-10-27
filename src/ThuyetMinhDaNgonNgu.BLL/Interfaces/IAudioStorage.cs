using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface IAudioStorage
{
    Task UploadAsync(
        string objectPath,
        GeneratedAudio audio,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectPath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default);

    Task<string> GetSignedUrlAsync(string objectPath, CancellationToken cancellationToken = default);
}
