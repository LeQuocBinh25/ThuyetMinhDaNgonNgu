
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<TouristLocation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TouristLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default);

    Task<TouristLocation?> UpdateAsync(
        int id,
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default);

    Task<LocationDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
