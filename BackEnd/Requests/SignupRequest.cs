using System.ComponentModel.DataAnnotations;

namespace Backend.Requests
{
    public class SignupRequest
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
    }
}
