namespace ThuyetMinhDaNgonNgu.API.DTOs.Responses;

/// <summary>Thông tin địa điểm được trả về bởi API.</summary>
public sealed record LocationResponse(
    int Id,
    string Name,
    string City,
    string Description,
    double? Latitude,
    double? Longitude);
