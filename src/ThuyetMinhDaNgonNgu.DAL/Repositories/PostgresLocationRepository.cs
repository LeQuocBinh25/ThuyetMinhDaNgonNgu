using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;
using ThuyetMinhDaNgonNgu.DAL.Entities;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

public sealed class PostgresLocationRepository(AppDbContext dbContext) : ILocationRepository
{
    public async Task<IReadOnlyList<TouristLocation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Locations.AsNoTracking()
            .OrderBy(location => location.Id)
            .Select(location => new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude))
            .ToListAsync(cancellationToken);

    public async Task<TouristLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Locations.AsNoTracking()
            .Where(location => location.Id == id)
            .Select(location => new TouristLocation(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TouristLocation> AddAsync(
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
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
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TouristLocation(entity.Id, entity.Name, entity.City, entity.Description, entity.Latitude, entity.Longitude);
    }

    public async Task<TouristLocation?> UpdateAsync(
        int id,
        string name,
        string city,
        string description,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Locations.FirstOrDefaultAsync(
            location => location.Id == id,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.City = city;
        entity.Description = description;
        entity.Latitude = latitude;
        entity.Longitude = longitude;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new TouristLocation(
            entity.Id,
            entity.Name,
            entity.City,
            entity.Description,
            entity.Latitude,
            entity.Longitude);
    }

    public async Task<LocationDeleteResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                bool exists = await dbContext.Locations
                    .AnyAsync(location => location.Id == id, cancellationToken);
                if (!exists)
                {
                    return new LocationDeleteResult(ResourceDeleteResult.NotFound, []);
                }

                var audioObjectPaths = await dbContext.Narrations
                    .Where(narration => narration.LocationId == id)
                    .Select(narration => narration.AudioObjectPath)
                    .ToListAsync(cancellationToken);

                await dbContext.Narrations
                    .Where(narration => narration.LocationId == id)
                    .ExecuteDeleteAsync(cancellationToken);

                int deleted = await dbContext.Locations
                    .Where(location => location.Id == id)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted == 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return new LocationDeleteResult(ResourceDeleteResult.NotFound, []);
                }

                await transaction.CommitAsync(cancellationToken);
                return new LocationDeleteResult(ResourceDeleteResult.Deleted, audioObjectPaths);
            }
            catch (PostgresException exception) when
                (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new LocationDeleteResult(ResourceDeleteResult.InUse, []);
            }
        });
    }
}
