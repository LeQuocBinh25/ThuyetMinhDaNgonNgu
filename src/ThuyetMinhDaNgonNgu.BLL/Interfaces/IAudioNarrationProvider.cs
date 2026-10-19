namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface IAudioNarrationProvider
{
    Task<byte[]> GenerateAudioAsync(string text);
}