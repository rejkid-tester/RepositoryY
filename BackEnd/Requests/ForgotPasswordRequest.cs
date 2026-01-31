using System.ComponentModel.DataAnnotations;

namespace Backend.Requests
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string? Dob { get; set; }
    }
}