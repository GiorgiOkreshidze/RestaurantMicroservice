using MongoDB.Driver;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class PreOrderRepository : IPreOrderRepository
{
    private readonly IMongoCollection<PreOrder> _collection;

    public PreOrderRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<PreOrder>("PreOrders");
        var indexKeysDefinition = Builders<PreOrder>.IndexKeys.Ascending(p => p.ReservationId);
        var indexOptions = new CreateIndexOptions { Name = "ReservationId_Index" };
        var indexModel = new CreateIndexModel<PreOrder>(indexKeysDefinition, indexOptions);
        _collection.Indexes.CreateOne(indexModel);
    }

    public async Task<List<PreOrder>> GetPreOrdersAsync(string userId, bool includeCancelled = false)
    {
        var filter = Builders<PreOrder>.Filter.Eq(p => p.UserId, userId);
        if (!includeCancelled)
        {
            filter &= Builders<PreOrder>.Filter.Ne(p => p.Status, "Cancelled");
        }

        var preOrders = await _collection.Find(filter).ToListAsync();
        return preOrders;
    }

    public async Task<PreOrder?> GetPreOrderByIdAsync(string userId, string preOrderId)
    {
        var filter = Builders<PreOrder>.Filter.And(
            Builders<PreOrder>.Filter.Eq(p => p.UserId, userId),
            Builders<PreOrder>.Filter.Eq(p => p.Id, preOrderId)
        );

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
    
    public async Task<PreOrder?> GetPreOrderOnlyByIdAsync(string preOrderId)
    {
        var filter = Builders<PreOrder>.Filter.Eq(p => p.Id, preOrderId);

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<PreOrder> CreatePreOrderAsync(PreOrder preOrder)
    {
        if (string.IsNullOrEmpty(preOrder.Id))
            preOrder.Id = Guid.NewGuid().ToString("N");

        if (preOrder.CreateDate == default)
            preOrder.CreateDate = DateTime.UtcNow;

        foreach (var item in preOrder.Items)
        {
            item.Id = Guid.NewGuid().ToString("N");
        }

        await _collection.InsertOneAsync(preOrder);
        return preOrder;
    }

    public async Task UpdatePreOrderAsync(PreOrder preOrder)
    {
        var existingPreOrder = await GetPreOrderByIdAsync(preOrder.UserId, preOrder.Id);
        if (existingPreOrder == null)
            throw new InvalidOperationException($"Pre-order with ID {preOrder.Id} not found");

        foreach (var item in preOrder.Items)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString("N");
        }

        var filter = Builders<PreOrder>.Filter.Eq(p => p.Id, preOrder.Id);
        await _collection.ReplaceOneAsync(filter, preOrder);
    }
    
    public async Task<List<PreOrderItem>> GetPreOrderItemsAsync(string preOrderId)
    {
        var filter = Builders<PreOrder>.Filter.Eq(p => p.Id, preOrderId);
        var preOrder = await _collection.Find(filter).FirstOrDefaultAsync();
        
        return preOrder?.Items ?? [];
    }

    public async Task UpdatePreOrderDishesStatusAsync(string preOrderId, string dishId, string dishStatus)
    {
        var filter = Builders<PreOrder>.Filter.And(
            Builders<PreOrder>.Filter.Eq(p => p.Id, preOrderId),
            Builders<PreOrder>.Filter.ElemMatch(p => p.Items, i => i.DishId == dishId)
        );

        var update = Builders<PreOrder>.Update.Set("Items.$.DishStatus", dishStatus);
        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<PreOrder?> GetPreOrderByReservationIdAsync(string reservationId)
    {
        var filter = Builders<PreOrder>.Filter.Eq(p => p.ReservationId, reservationId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}
