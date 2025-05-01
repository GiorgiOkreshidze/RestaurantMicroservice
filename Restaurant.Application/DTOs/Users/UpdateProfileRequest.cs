namespace Restaurant.Application.DTOs.Users;

public class UpdateProfileRequest
{
    public required string FirstName { get; set; }
    
    public required string LastName { get; set; }
    
    public string? Base64EncodedImage { get; set; }
}