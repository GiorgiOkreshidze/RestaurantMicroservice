using MongoDB.Driver;
using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;
using SortDirection = Restaurant.Domain.Entities.Enums.SortDirection;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IMongoDatabase database) : IDishRepository
{
    private readonly IMongoCollection<Dish> _dishes = database.GetCollection<Dish>("Dishes");

    public async Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId)
    {
        var filter = Builders<Dish>.Filter.And(
            Builders<Dish>.Filter.Eq(d => d.LocationId, locationId),
            Builders<Dish>.Filter.Eq(d => d.IsPopular, true)
        );

        return await _dishes.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Dish>> GetPopularDishesAsync()
    {
        var filter = Builders<Dish>.Filter.Eq(d => d.IsPopular, true);
        return await _dishes.Find(filter).ToListAsync();
    }

    public async Task<Dish?> GetDishByIdAsync(string id)
    {
        var filter = Builders<Dish>.Filter.Eq(d => d.Id, id);
        return await _dishes.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Dish>> GetAllDishesAsync(DishFilterDto filter)
    {
        var queryFilter = Builders<Dish>.Filter.Empty;

        if (filter.DishType is not null)
        {
            var dishTypeString = filter.DishType.ToString();
            queryFilter = Builders<Dish>.Filter.Eq(d => d.DishType, dishTypeString);
        }
        
        SortDefinition<Dish> sortDefinition = null;
        var isDescending = filter.SortDirection == SortDirection.Desc;

        sortDefinition = filter.SortBy switch
        {
            DishSortBy.Price => isDescending 
                ? Builders<Dish>.Sort.Descending("Price") 
                : Builders<Dish>.Sort.Ascending("Price"),
            DishSortBy.IsPopular => isDescending 
                ? Builders<Dish>.Sort.Descending("IsPopular") 
                : Builders<Dish>.Sort.Ascending("IsPopular"),
            _ => Builders<Dish>.Sort.Ascending("Id")
        };
        
        return await _dishes.Find(queryFilter)
            .Sort(sortDefinition)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds)
    {
        if (dishIds == null || !dishIds.Any())
            return new List<Dish>();

        var filter = Builders<Dish>.Filter.In(d => d.Id, dishIds);
        return await _dishes.Find(filter).ToListAsync();
    }
}