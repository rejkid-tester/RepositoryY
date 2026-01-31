using System.ComponentModel.DataAnnotations;

namespace Backend.Requests
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        public DateTime Dob { get; set; }
        
        public DateTime Ts { get; set; }
        
        // MFA - Optional phone number for two-factor authentication
        [Phone]
        public string? PhoneNumber { get; set; }
        
        // MFA - Enable MFA during registration (requires PhoneNumber)
        public bool EnableMfa { get; set; } = false;
    }
}