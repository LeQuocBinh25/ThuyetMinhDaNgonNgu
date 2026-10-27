using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class NarrationService : INarrationService
{
    private const string CurrentAudioPathPrefix = "narrations/v2/";

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
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var (location, language) = await GetSelectionAsync(locationId, languageCode, cancellationToken);
        var current = await _narrations.GetCurrentAsync(location.Id, language.Code, cancellationToken);

        if (current is not null &&
            !string.IsNullOrWhiteSpace(current.Text) &&
            current.AudioObjectPath.StartsWith(CurrentAudioPathPrefix, StringComparison.Ordinal) &&
            await _audioStorage.ExistsAsync(current.AudioObjectPath, cancellationToken))
        {
            return await ToResultAsync(current, isCached: true, cancellationToken);
        }

        string text = current is not null && !string.IsNullOrWhiteSpace(current.Text)
            ? current.Text
            : await _aiProvider.GenerateNarrationAsync(
            location.Name,
            $"{location.City}. {location.Description}",
            language.Code,
            cancellationToken);

        return await PublishTextAsync(
            location,
            language,
            text,
            current is null || string.IsNullOrWhiteSpace(current.Text) ? "ai" : current.Source,
            current,
            isCached: false,
            cancellationToken);
    }

    public async Task<string> CreateDraftAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var (location, language) = await GetSelectionAsync(locationId, languageCode, cancellationToken);
        return await _aiProvider.GenerateNarrationAsync(
            location.Name,
            $"{location.City}. {location.Description}",
            language.Code,
            cancellationToken);
    }

    public async Task<NarrationResult> PublishAsync(
        int locationId,
        string languageCode,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Nội dung thuyết minh không được để trống.", nameof(text));
        }

        if (text.Length > 50000)
        {
            throw new ArgumentException("Nội dung thuyết minh vượt quá 50000 ký tự.", nameof(text));
        }

        var (location, language) = await GetSelectionAsync(locationId, languageCode, cancellationToken);
        var current = await _narrations.GetCurrentAsync(location.Id, language.Code, cancellationToken);
        return await PublishTextAsync(
            location,
            language,
            text.Trim(),
            "admin",
            current,
            isCached: false,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NarrationResult>> GetAllCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _narrations.GetAllCurrentAsync(cancellationToken);
        return items.Select(item => ToResult(item, isCached: true, audioUrl: null)).ToArray();
    }

    public async Task<NarrationResult?> GetCurrentAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var (_, language) = await GetSelectionAsync(locationId, languageCode, cancellationToken);
        var item = await _narrations.GetCurrentAsync(locationId, language.Code, cancellationToken);
        return item is null ? null : await ToResultAsync(item, isCached: true, cancellationToken);
    }

    public async Task DeleteAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var (_, language) = await GetSelectionAsync(locationId, languageCode, cancellationToken);
        var narration = await _narrations.DeleteCurrentAsync(
            locationId,
            language.Code,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Không tìm thấy bản thuyết minh hiện hành cho địa điểm và ngôn ngữ này.");

        if (await _narrations.IsAudioObjectReferencedAsync(
                narration.AudioObjectPath,
                cancellationToken))
        {
            return;
        }

        try
        {
            await _audioStorage.DeleteAsync(narration.AudioObjectPath, CancellationToken.None);
        }
        catch (HttpRequestException exception)
        {
            throw new HttpRequestException(
                "Bản thuyết minh đã bị xóa nhưng không thể dọn tệp audio. Cần kiểm tra và dọn Storage.",
                exception,
                exception.StatusCode);
        }
    }

    private async Task<(TouristLocation Location, SupportedLanguage Language)> GetSelectionAsync(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        if (locationId <= 0)
        {
            throw new ArgumentException("Mã địa điểm phải là số nguyên dương.", nameof(locationId));
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Mã ngôn ngữ không được để trống.", nameof(languageCode));
        }

        var location = await _locations.GetByIdAsync(locationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy địa điểm có mã {locationId}.");
        string normalizedCode = languageCode.Trim().ToLowerInvariant();
        var language = await _languages.GetByCodeAsync(normalizedCode, cancellationToken)
            ?? throw new ArgumentException($"Ngôn ngữ '{languageCode}' chưa được hỗ trợ.", nameof(languageCode));
        return (location, language);
    }

    private async Task<NarrationResult> PublishTextAsync(
        TouristLocation location,
        SupportedLanguage language,
        string text,
        string source,
        NarrationRecord? previous,
        bool isCached,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Nhà cung cấp AI không trả về nội dung thuyết minh.");
        }

        if (text.Length > 50000)
        {
            throw new InvalidOperationException("Nội dung do AI tạo vượt quá giới hạn lưu trữ.");
        }

        var audio = await _audioProvider.GenerateAudioAsync(text, language.Code, cancellationToken);
        string objectPath = $"{CurrentAudioPathPrefix}{location.Id}/{Guid.NewGuid():N}{audio.Extension}";
        await _audioStorage.UploadAsync(objectPath, audio, cancellationToken);

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
            saved = await _narrations.SaveCurrentAsync(candidate, cancellationToken);
        }
        catch (Exception persistenceException)
        {
            try
            {
                await _audioStorage.DeleteAsync(objectPath, CancellationToken.None);
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

        if (previous is not null &&
            !string.Equals(previous.AudioObjectPath, saved.AudioObjectPath, StringComparison.Ordinal) &&
            !await _narrations.IsAudioObjectReferencedAsync(
                previous.AudioObjectPath,
                cancellationToken))
        {
            try
            {
                await _audioStorage.DeleteAsync(previous.AudioObjectPath, CancellationToken.None);
            }
            catch (HttpRequestException exception)
            {
                throw new HttpRequestException(
                    "Bản thuyết minh mới đã được lưu nhưng không thể dọn tệp audio cũ. Cần kiểm tra và dọn Storage.",
                    exception,
                    exception.StatusCode);
            }
        }

        return await ToResultAsync(saved, isCached, cancellationToken);
    }

    private async Task<NarrationResult> ToResultAsync(
        NarrationRecord item,
        bool isCached,
        CancellationToken cancellationToken) =>
        ToResult(
            item,
            isCached,
            await _audioStorage.GetSignedUrlAsync(item.AudioObjectPath, cancellationToken));

    private static NarrationResult ToResult(
        NarrationRecord item,
        bool isCached,
        string? audioUrl) =>
        new(
            item.Id.ToString("D"),
            item.LocationId,
            item.LocationName,
            item.LanguageCode,
            item.LanguageName,
            item.Text,
            audioUrl,
            "completed",
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            isCached);
}
