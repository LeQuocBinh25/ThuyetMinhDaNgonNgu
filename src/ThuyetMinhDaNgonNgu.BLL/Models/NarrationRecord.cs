namespace ThuyetMinhDaNgonNgu.BLL.Models;

public sealed record NarrationRecord(
    Guid Id,
    int LocationId,
    string LocationName,
    string LanguageCode,
    string LanguageName,
    string Text,
    string AudioObjectPath,
    string AudioContentType,
    string Source,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
