using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IPreOrderRepository
{
    public Task<List<PreOrder>> GetPreOrdersAsync(string userId, bool includeCancelled = false);
    
    public Task<PreOrder?> GetPreOrderByIdAsync(string userId, string preOrderId);
    
    public Task<PreOrder> CreatePreOrderAsync(PreOrder preOrder);
    
    Task UpdatePreOrderAsync(PreOrder preOrder);
}
