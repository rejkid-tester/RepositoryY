using System.ComponentModel.DataAnnotations;

namespace Backend.Requests
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public string? Dob { get; set; }
    }
}