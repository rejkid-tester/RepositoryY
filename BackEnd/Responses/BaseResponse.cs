using System.Text.Json.Serialization;

namespace Backend.Responses
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
    }
}
