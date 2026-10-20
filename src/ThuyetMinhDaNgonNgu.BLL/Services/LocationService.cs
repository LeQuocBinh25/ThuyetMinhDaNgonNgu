
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

    public IReadOnlyList<TouristLocation> GetAll() =>
        _repository.GetAll();

    public TouristLocation? GetById(int id) =>
        _repository.GetById(id);

    public Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên địa điểm không được để trống.");

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Tên thành phố không được để trống.");

        return _repository.AddAsync(
            name.Trim(),
            city.Trim(),
            description?.Trim() ?? string.Empty);
    }
}
