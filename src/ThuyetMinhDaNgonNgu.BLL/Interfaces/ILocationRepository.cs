
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<TouristLocation>> GetAllAsync();

    Task<TouristLocation?> GetByIdAsync(int id);

    Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude);
}
