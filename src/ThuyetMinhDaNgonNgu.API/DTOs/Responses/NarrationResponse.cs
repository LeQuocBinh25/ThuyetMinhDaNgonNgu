namespace ThuyetMinhDaNgonNgu.API.DTOs.Responses;

public sealed record NarrationResponse(
    string Id,
    int LocationId,
    string LocationName,
    string LanguageCode,
    string LanguageName,
    string Text,
    string? AudioUrl,
    string Status,
    DateTimeOffset CreatedAtUtc);
