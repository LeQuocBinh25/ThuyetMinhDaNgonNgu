
using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

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
            : new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description);
    }

    public async Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description)
    {
        var entity = new LocationEntity
        {
            Name = name,
            City = city,
            Description = description
        };

        _dbContext.Locations.Add(entity);
        await _dbContext.SaveChangesAsync();

        return new TouristLocation(
            entity.Id,
            entity.Name,
            entity.City,
            entity.Description);
    }
}
