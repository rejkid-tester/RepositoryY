namespace Backend.Responses
{
    public class UserResponse : BaseResponse
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreationDate { get; set; }
        
        // MFA properties
        public bool MfaEnabled { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
