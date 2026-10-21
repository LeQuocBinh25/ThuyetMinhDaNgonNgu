
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILocationService
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
