namespace Restaurant.Application.DTOs.Aws;

public class SesEmailSettings
{
    public string FromEmail { get; set; } = string.Empty;
    
    public string ToEmail { get; set; } = string.Empty;
}