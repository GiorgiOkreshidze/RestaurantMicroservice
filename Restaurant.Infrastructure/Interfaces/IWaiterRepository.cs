using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IWaiterRepository
{
    Task<List<User>> GetWaitersByLocationAsync(string locationId);
}