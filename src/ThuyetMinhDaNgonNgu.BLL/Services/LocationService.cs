
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

    public Task<IReadOnlyList<TouristLocation>> GetAllAsync() =>
        _repository.GetAllAsync();

    public Task<TouristLocation?> GetByIdAsync(int id) =>
        _repository.GetByIdAsync(id);

    public Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên địa điểm không được để trống.");

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Tên thành phố không được để trống.");

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentException("Tọa độ địa lý không hợp lệ.");

        if (latitude.HasValue != longitude.HasValue)
            throw new ArgumentException("Vĩ độ và kinh độ phải được cung cấp cùng nhau.");

        return _repository.AddAsync(
            name.Trim(),
            city.Trim(),
            description?.Trim() ?? string.Empty,
            latitude,
            longitude);
    }
}
