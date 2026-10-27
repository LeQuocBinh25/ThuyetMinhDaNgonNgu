namespace ThuyetMinhDaNgonNgu.BLL.Models;

public sealed record NarrationResult(
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
