using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Interfaces;

public interface IEmployeeRepository
{
    Task<EmployeeInfo?> GetWaiterByEmailAsync(string email);
}