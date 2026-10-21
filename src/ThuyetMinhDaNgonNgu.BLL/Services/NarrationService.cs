using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class NarrationService : INarrationService
{
    private readonly ILocationRepository _locations;
    private readonly ILanguageRepository _languages;
    private readonly INarrationRepository _narrations;
    private readonly IAiNarrationProvider _aiProvider;
    private readonly IAudioNarrationProvider _audioProvider;
    private readonly IAudioStorage _audioStorage;

    public NarrationService(
        ILocationRepository locations,
        ILanguageRepository languages,
        INarrationRepository narrations,
        IAiNarrationProvider aiProvider,
        IAudioNarrationProvider audioProvider,
        IAudioStorage audioStorage)
    {
        _locations = locations;
        _languages = languages;
        _narrations = narrations;
        _aiProvider = aiProvider;
        _audioProvider = audioProvider;
        _audioStorage = audioStorage;
    }

    public async Task<NarrationResult> CreateNarrationAsync(
        int locationId,
        string languageCode)
    {
        var (location, language) = await GetSelectionAsync(locationId, languageCode);
        var current = await _narrations.GetCurrentAsync(location.Id, language.Code);

        if (current is not null &&
            !string.IsNullOrWhiteSpace(current.Text) &&
            await _audioStorage.ExistsAsync(current.AudioObjectPath))
        {
            return ToResult(current, isCached: true);
        }

        string text = current is not null && !string.IsNullOrWhiteSpace(current.Text)
            ? current.Text
            : await _aiProvider.GenerateNarrationAsync(
            location.Name,
            $"{location.City}. {location.Description}",
            language.Code);

        return await PublishTextAsync(
            location,
            language,
            text,
            current is null || string.IsNullOrWhiteSpace(current.Text) ? "ai" : current.Source,
            current,
            isCached: false);
    }

    public async Task<string> CreateDraftAsync(int locationId, string languageCode)
    {
        var (location, language) = await GetSelectionAsync(locationId, languageCode);
        return await _aiProvider.GenerateNarrationAsync(
            location.Name,
            $"{location.City}. {location.Description}",
            language.Code);
    }

    public async Task<NarrationResult> PublishAsync(
        int locationId,
        string languageCode,
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Nội dung thuyết minh không được để trống.", nameof(text));
        }

        if (text.Length > 50000)
        {
            throw new ArgumentException("Nội dung thuyết minh vượt quá 50000 ký tự.", nameof(text));
        }

        var (location, language) = await GetSelectionAsync(locationId, languageCode);
        var current = await _narrations.GetCurrentAsync(location.Id, language.Code);
        return await PublishTextAsync(
            location,
            language,
            text.Trim(),
            "admin",
            current,
            isCached: false);
    }

    public async Task<IReadOnlyList<NarrationResult>> GetAllCurrentAsync()
    {
        var items = await _narrations.GetAllCurrentAsync();
        return items.Select(item => ToResult(item, isCached: true)).ToArray();
    }

    public async Task<NarrationResult?> GetCurrentAsync(int locationId, string languageCode)
    {
        var (_, language) = await GetSelectionAsync(locationId, languageCode);
        var item = await _narrations.GetCurrentAsync(locationId, language.Code);
        return item is null ? null : ToResult(item, isCached: true);
    }

    private async Task<(TouristLocation Location, SupportedLanguage Language)> GetSelectionAsync(
        int locationId,
        string languageCode)
    {
        if (locationId <= 0)
        {
            throw new ArgumentException("Mã địa điểm phải là số nguyên dương.", nameof(locationId));
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Mã ngôn ngữ không được để trống.", nameof(languageCode));
        }

        var location = await _locations.GetByIdAsync(locationId)
            ?? throw new KeyNotFoundException($"Không tìm thấy địa điểm có mã {locationId}.");
        string normalizedCode = languageCode.Trim().ToLowerInvariant();
        var language = await _languages.GetByCodeAsync(normalizedCode)
            ?? throw new ArgumentException($"Ngôn ngữ '{languageCode}' chưa được hỗ trợ.", nameof(languageCode));
        return (location, language);
    }

    private async Task<NarrationResult> PublishTextAsync(
        TouristLocation location,
        SupportedLanguage language,
        string text,
        string source,
        NarrationRecord? previous,
        bool isCached)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Nhà cung cấp AI không trả về nội dung thuyết minh.");
        }

        if (text.Length > 50000)
        {
            throw new InvalidOperationException("Nội dung do AI tạo vượt quá giới hạn lưu trữ.");
        }

        var audio = await _audioProvider.GenerateAudioAsync(text, language.Code);
        string objectPath = $"narrations/{location.Id}/{Guid.NewGuid():N}{audio.Extension}";
        await _audioStorage.UploadAsync(objectPath, audio);

        var now = DateTimeOffset.UtcNow;
        var candidate = new NarrationRecord(
            previous?.Id ?? Guid.NewGuid(),
            location.Id,
            location.Name,
            language.Code,
            language.Name,
            text,
            objectPath,
            audio.ContentType,
            source,
            previous?.CreatedAtUtc ?? now,
            now);

        NarrationRecord saved;
        try
        {
            saved = await _narrations.SaveCurrentAsync(candidate);
        }
        catch (Exception persistenceException)
        {
            try
            {
                await _audioStorage.DeleteAsync(objectPath);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Lưu bản thuyết minh thất bại và không thể dọn audio chưa được tham chiếu.",
                    persistenceException,
                    cleanupException);
            }

            throw;
        }

        return ToResult(saved, isCached);
    }

    private NarrationResult ToResult(NarrationRecord item, bool isCached) =>
        new(
            item.Id.ToString("D"),
            item.LocationId,
            item.LocationName,
            item.LanguageCode,
            item.LanguageName,
            item.Text,
            _audioStorage.GetPublicUrl(item.AudioObjectPath),
            "completed",
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            isCached);
}
