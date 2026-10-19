using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

/// <summary>Truy cập địa điểm được lưu trong SQLite và chuyển Entity thành model của BLL.</summary>
public sealed class SqliteLocationRepository : ILocationRepository
{
    private readonly AppDbContext _dbContext;

    public SqliteLocationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<TouristLocation> GetAll() =>
        _dbContext.Locations
            .AsNoTracking()
            .OrderBy(location => location.Id)
            .ToList()
            .Select(location => new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description))
            .ToArray();

    public TouristLocation? GetById(int id)
    {
        var location = _dbContext.Locations
            .AsNoTracking()
            .FirstOrDefault(item => item.Id == id);

        return location is null
            ? null
            : new TouristLocation(location.Id, location.Name, location.City, location.Description);
    }
}
