using System.ComponentModel.DataAnnotations;

namespace Restaurant.Application.DTOs.Users
{
    public class UpdatePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
