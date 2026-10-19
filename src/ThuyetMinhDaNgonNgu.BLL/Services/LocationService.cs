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

    public IReadOnlyList<TouristLocation> GetAll() => _repository.GetAll();

    public TouristLocation? GetById(int id) => _repository.GetById(id);
}
