using System.Net;
using System.Net.Mail;
using System.Text;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class EmailService(IUserRepository userRepository) : IEmailService
{
    public async Task SendPreOrderConfirmationEmailAsync(string userId, PreOrder preOrder)
    {
        // Validation code remains the same
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty");

        if (preOrder == null)
            throw new ArgumentNullException(nameof(preOrder), "Pre-order cannot be null");

        var user = await userRepository.GetUserByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Email))
            throw new InvalidOperationException($"Cannot send pre-order confirmation: User {userId} not found or has no email");

        var subject = "Your Restaurant Pre-Order Confirmation";
        var body = BuildSimpleEmailBody(preOrder);

        // Use Gmail's SMTP for testing
        using var client = new SmtpClient();
        client.Host = "sandbox.smtp.mailtrap.io";
        client.Port = 2525;
        client.EnableSsl = true;
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.Credentials = new NetworkCredential("53bc63a8682e2c", "0403e227535a0f");

        using var message = new MailMessage();
        message.From = new MailAddress("example@example.com", "Restaurant Booking");
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = false;

        // Add the temp-mail address received from your user
        message.To.Add(user.Email);

        await client.SendMailAsync(message);
    }

    private static string BuildSimpleEmailBody(PreOrder preOrder)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Thank you for your pre-order!");
        sb.AppendLine();
        sb.AppendLine("Order Details:");
        sb.AppendLine($"- Order ID: {preOrder.PreOrderId}");
        sb.AppendLine($"- Reservation Date: {preOrder.ReservationDate}");
        sb.AppendLine($"- Time: {preOrder.TimeSlot}");
        sb.AppendLine($"- Location: {preOrder.Address}");
        sb.AppendLine($"- Status: {preOrder.Status}");
        sb.AppendLine();

        sb.AppendLine("Ordered Items:");
        foreach (var item in preOrder.Items)
        {
            sb.AppendLine($"- {item.DishName} x{item.Quantity} - ${item.Price:F2} each");
        }

        sb.AppendLine();
        sb.AppendLine($"Total: ${preOrder.TotalPrice:F2}");
        sb.AppendLine();
        sb.AppendLine("Thank you for choosing our restaurant!");

        return sb.ToString();
    }
}