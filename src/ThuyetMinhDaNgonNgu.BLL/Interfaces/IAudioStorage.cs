using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface IAudioStorage
{
    Task UploadAsync(string objectPath, GeneratedAudio audio);

    Task<bool> ExistsAsync(string objectPath);

    Task DeleteAsync(string objectPath);

    string GetPublicUrl(string objectPath);
}
