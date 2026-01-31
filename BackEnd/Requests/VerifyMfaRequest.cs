
namespace Backend.Requests
{
    public class VerifyMfaRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}

