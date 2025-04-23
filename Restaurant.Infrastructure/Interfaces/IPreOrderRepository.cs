using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IPreOrderRepository
{
    public Task<List<PreOrder>> GetPreOrdersAsync(string userId);
}
