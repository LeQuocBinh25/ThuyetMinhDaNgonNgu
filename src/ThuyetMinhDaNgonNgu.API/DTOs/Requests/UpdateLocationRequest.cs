using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

/// <summary>Thông tin dùng để cập nhật một địa điểm.</summary>
public sealed record UpdateLocationRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string City { get; init; }

    [StringLength(4000)]
    public string Description { get; init; } = string.Empty;

    [Range(-90, 90)]
    public double? Latitude { get; init; }

    [Range(-180, 180)]
    public double? Longitude { get; init; }
}
