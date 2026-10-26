using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

public sealed class PostgresLocationRepository(AppDbContext dbContext) : ILocationRepository
{
    public async Task<IReadOnlyList<TouristLocation>> GetAllAsync() =>
        await dbContext.Locations.AsNoTracking()
            .OrderBy(location => location.Id)
            .Select(location => new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude))
            .ToListAsync();

    public async Task<TouristLocation?> GetByIdAsync(int id) =>
        await dbContext.Locations.AsNoTracking()
            .Where(location => location.Id == id)
            .Select(location => new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude))
            .FirstOrDefaultAsync();

    public async Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude)
    {
        var entity = new LocationEntity
        {
            Name = name,
            City = city,
            Description = description,
            Latitude = latitude,
            Longitude = longitude
        };

        dbContext.Locations.Add(entity);
        await dbContext.SaveChangesAsync();
        return new TouristLocation(entity.Id, entity.Name, entity.City, entity.Description, entity.Latitude, entity.Longitude);
    }
}
