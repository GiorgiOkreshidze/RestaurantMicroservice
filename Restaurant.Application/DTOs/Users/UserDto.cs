namespace Restaurant.Application.DTOs.Users;

public class UserDto
{
    public string? Id { get; set; }
    
    public required string Email { get; set; }
    
    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    
    public string? LocationId { get; set; }
    
    public string GetFullName()
    {
        return $"{FirstName} {LastName}";
    }
}