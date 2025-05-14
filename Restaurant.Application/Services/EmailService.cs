using System.Text;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using Restaurant.Application.DTOs.Aws;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using Message = Amazon.SimpleEmail.Model.Message;

namespace Restaurant.Application.Services;


public class EmailService(IUserRepository userRepository, IAmazonSimpleEmailService sesClient, IOptions<SesEmailSettings> settings) : IEmailService
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

        var sendRequest = new SendEmailRequest
        {
            Source = settings.Value.FromEmail, // Verified email in SES
            Destination = new Destination
            {
                ToAddresses = [settings.Value.ToEmail]
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = body
                    }
                }
            }
        };

        try
        {
            await sesClient.SendEmailAsync(sendRequest);
        }
        catch (AmazonSimpleEmailServiceException ex)
        {
            throw new InvalidOperationException($"Failed to send email via SES: {ex.Message}", ex);
        }
    }

    private static string BuildSimpleEmailBody(PreOrder preOrder)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Thank you for your pre-order!");
        sb.AppendLine();
        sb.AppendLine("Order Details:");
        sb.AppendLine($"- Order ID: {preOrder.Id}");
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