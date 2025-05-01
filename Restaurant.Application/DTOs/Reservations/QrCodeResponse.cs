namespace Restaurant.Application.DTOs.Reservations;

public class QrCodeResponse
{
    public required string QrCodeImageBase64 { get; set; } 
    
    public required string FeedbackUrl { get; set; }
}