
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Services;

public sealed class LocationService : ILocationService
{
    private readonly ILocationRepository _repository;

    public LocationService(ILocationRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<TouristLocation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<TouristLocation?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        ValidateLocation(name, city, description, latitude, longitude);

        return _repository.AddAsync(
            name.Trim(),
            city.Trim(),
            description?.Trim() ?? string.Empty,
            latitude,
            longitude,
            cancellationToken);
    }

    public Task<TouristLocation?> UpdateAsync(
        int id,
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        ValidateLocation(name, city, description, latitude, longitude);
        if (id <= 0)
        {
            throw new ArgumentException("Mã địa điểm phải là số nguyên dương.", nameof(id));
        }

        return _repository.UpdateAsync(
            id,
            name.Trim(),
            city.Trim(),
            description?.Trim() ?? string.Empty,
            latitude,
            longitude,
            cancellationToken);
    }

    public Task<ResourceDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Mã địa điểm phải là số nguyên dương.", nameof(id));
        }

        return _repository.DeleteAsync(id, cancellationToken);
    }

    private static void ValidateLocation(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên địa điểm không được để trống.", nameof(name));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Tên thành phố không được để trống.", nameof(city));

        if (name.Trim().Length > 200)
            throw new ArgumentException("Tên địa điểm không được vượt quá 200 ký tự.", nameof(name));

        if (city.Trim().Length > 200)
            throw new ArgumentException("Tên thành phố không được vượt quá 200 ký tự.", nameof(city));

        if (description.Length > 4000)
            throw new ArgumentException("Mô tả địa điểm không được vượt quá 4000 ký tự.", nameof(description));

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentException("Tọa độ địa lý không hợp lệ.");

        if (latitude.HasValue != longitude.HasValue)
            throw new ArgumentException("Vĩ độ và kinh độ phải được cung cấp cùng nhau.");
    }
}
