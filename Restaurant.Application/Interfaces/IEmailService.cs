using Restaurant.Domain.Entities;

namespace Restaurant.Application.Interfaces;

public interface IEmailService
{
    public Task SendPreOrderConfirmationEmailAsync(string userId, PreOrder preOrder);
}