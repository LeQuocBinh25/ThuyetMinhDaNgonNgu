namespace ThuyetMinhDaNgonNgu.DAL.Entities;

/// <summary>Entity lưu thông tin địa điểm trong bảng Locations của SQLite.</summary>
public sealed class LocationEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
