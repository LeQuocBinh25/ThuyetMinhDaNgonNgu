namespace ThuyetMinhDaNgonNgu.API.DTOs.Responses;

/// <summary>Thông tin ngôn ngữ được trả về bởi API.</summary>
public sealed record LanguageResponse(
    string Code,
    string Name);
