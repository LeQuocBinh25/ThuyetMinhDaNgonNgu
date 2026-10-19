
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class NarrationService : INarrationService
{
    private readonly ILocationRepository _locations;
    private readonly ILanguageRepository _languages;
    private readonly IAiNarrationProvider _aiProvider;
    private readonly IAudioNarrationProvider _audioProvider;

    public NarrationService(
        ILocationRepository locations,
        ILanguageRepository languages,
        IAiNarrationProvider aiProvider,
        IAudioNarrationProvider audioProvider)
    {
        _locations = locations;
        _languages = languages;
        _aiProvider = aiProvider;
        _audioProvider = audioProvider;
    }

    public async Task<NarrationResult> CreateNarrationAsync(
        int locationId,
        string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException(
                "Mã ngôn ngữ không được để trống.",
                nameof(languageCode));
        }

        var location = _locations.GetById(locationId)
            ?? throw new KeyNotFoundException(
                $"Không tìm thấy địa điểm có mã {locationId}.");

        var normalizedCode = languageCode.Trim().ToLowerInvariant();

        var language = _languages.GetByCode(normalizedCode)
            ?? throw new ArgumentException(
                $"Ngôn ngữ '{languageCode}' chưa được hỗ trợ.",
                nameof(languageCode));

        var text = await _aiProvider.GenerateNarrationAsync(
            location.Name,
            $"{location.City}. {location.Description}",
            language.Code);

        var audioBytes = await _audioProvider.GenerateAudioAsync(text);

        string folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "audio");

        Directory.CreateDirectory(folder);

        string id = Guid.NewGuid().ToString("N");
        string fileName = $"{id}.wav";
        string filePath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(filePath, audioBytes);

        return new NarrationResult(
            Id: id,
            LocationId: location.Id,
            LocationName: location.Name,
            LanguageCode: language.Code,
            LanguageName: language.Name,
            Text: text,
            AudioUrl: $"/audio/{fileName}",
            Status: "completed",
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }
}
