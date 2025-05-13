using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _collection;
    
    public OrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Order>("Orders");
        
        var reservationIdIndexKeys = Builders<Order>.IndexKeys
            .Ascending(r => r.ReservationId);
        var reservationIdIndexOptions = new CreateIndexOptions
        {
            Name = "ReservationId_Index"
        };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>(reservationIdIndexKeys, reservationIdIndexOptions));
    }

    public async Task<Order?> GetOrderByReservationIdAsync(string reservationId)
    {
        return await _collection
            .Find(order => order.ReservationId == reservationId)
            .FirstOrDefaultAsync();
    }

    public async Task<decimal> GetOrderRevenueByReservationIdAsync(string reservationId)
    {
        var order = await _collection
            .FindAsync(order => order.ReservationId == reservationId).Result
            .FirstOrDefaultAsync();

        return order?.TotalPrice ?? 0m;
    }

    public async Task SaveAsync(Order order)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
        var options = new ReplaceOptions { IsUpsert = true };
        
        await _collection.ReplaceOneAsync(filter, order, options);
    }
}