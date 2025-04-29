using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class OrderRepository(IDynamoDBContext context) : IOrderRepository
{
    public async Task<Order?> GetOrderByReservationIdAsync(string reservationId)
    {
        var query = context.QueryAsync<Order>(reservationId, new DynamoDBOperationConfig
        {
            IndexName = "ReservationIdIndex"
        });

        var orders = await query.GetNextSetAsync();
        var order = orders.FirstOrDefault();
        
        return order;
    }
    
    public async Task SaveAsync(Order order)
    {
        await context.SaveAsync(order);
    }
}