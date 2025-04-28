using System.ComponentModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class DishRepository(IDynamoDBContext context) : IDishRepository
{
    public async Task<IEnumerable<Dish>> GetSpecialtyDishesByLocationAsync(string locationId)
    {
        var query = context.QueryAsync<Dish>(locationId, new DynamoDBOperationConfig
        {
            IndexName = "GSI1"
        });

        var dishes = await query.GetRemainingAsync();
        return dishes.Where(d => d.IsPopular);
    }

    public async Task<IEnumerable<Dish>> GetPopularDishesAsync()
    {
        var condition = new ScanCondition("IsPopular", ScanOperator.Equal, true);
        var dishes = await context.ScanAsync<Dish>(
            new List<ScanCondition> { condition },
            new DynamoDBOperationConfig
            {
                Conversion = DynamoDBEntryConversion.V2
            }).GetRemainingAsync();
        
        return dishes;
    }

    public async Task<Dish?> GetDishByIdAsync(string id)
    {
        var dish = await context.LoadAsync<Dish>(id);
        return dish ?? null;    
    }

    public async Task<IEnumerable<Dish>> GetAllDishesAsync(DishFilterDto filter)
    {
        var dishes = await context.ScanAsync<Dish>(new List<ScanCondition>(), new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();

        if (filter.DishType is not null)
        {
            dishes = FilterDishByType(dishes, filter.DishType);
        }
        
        dishes = ApplySorting(dishes, filter.SortBy, filter.SortDirection).ToList();

        return dishes;
    }

    public async Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds)
    {
        if (dishIds == null || !dishIds.Any())
            return new List<Dish>();
        
        var dishes = new List<Dish>();
        foreach (var id in dishIds)
        {
            var dish = await context.LoadAsync<Dish>(id);
            if (dish != null)
                dishes.Add(dish);
        }
    
        return dishes;
    }

    private IEnumerable<Dish> ApplySorting(IEnumerable<Dish> dishes, DishSortBy? sortBy, SortDirection? sortDirection)
    {
        bool descending = sortDirection == SortDirection.Desc;

        return sortBy switch
        {
            DishSortBy.Price => descending
                ? dishes.OrderByDescending(d => ParsePrice(d.Price))
                : dishes.OrderBy(d => ParsePrice(d.Price)),

            DishSortBy.IsPopular => descending
                ? dishes.OrderByDescending(d => d.IsPopular)
                : dishes.OrderBy(d => d.IsPopular),

            _ => dishes // No sorting
        };
    }

    private decimal ParsePrice(string price)
    {
        return decimal.TryParse(price, out var parsedPrice) ? parsedPrice : 0;
    }
    
    private List<Dish> FilterDishByType(List<Dish> dishes, DishType? dishTypeEnum)
    {
        return dishTypeEnum switch
        {
            DishType.Appetizers => dishes
                .Where(dish => dish.DishType == nameof(DishType.Appetizers)).ToList(),
            DishType.Desserts => dishes
                .Where(dish => dish.DishType == nameof(DishType.Desserts)).ToList(),
            DishType.MainCourses => dishes
                .Where(dish => dish.DishType == nameof(DishType.MainCourses)).ToList(),
            _ => dishes
        };
    }
}