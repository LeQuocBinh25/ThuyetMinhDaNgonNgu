namespace ThuyetMinhDaNgonNgu.API.DTOs.Responses;

public sealed record AdminTokenResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
