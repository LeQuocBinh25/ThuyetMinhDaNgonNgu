//quy định Service phải có chức năng thêm địa điểm
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILocationService
{
    Task<IReadOnlyList<TouristLocation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TouristLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        int narrationRadiusMeters = 50,
        CancellationToken cancellationToken = default);

    Task<TouristLocation?> UpdateAsync(
        int id,
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        int narrationRadiusMeters = 50,
        CancellationToken cancellationToken = default);

    Task<ResourceDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
