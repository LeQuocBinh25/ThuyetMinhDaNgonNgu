namespace ThuyetMinhDaNgonNgu.API.DTOs.Responses;

/// <summary>Bản thuyết minh hiện hành và URL audio có chữ ký ngắn hạn nếu được yêu cầu.</summary>
public sealed record NarrationResponse(
    string Id,
    int LocationId,
    string LocationName,
    string LanguageCode,
    string LanguageName,
    string Text,
    string? AudioUrl,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool IsCached);
