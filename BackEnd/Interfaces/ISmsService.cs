// BackEnd/Interfaces/ISmsService.cs
namespace BackEnd.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendMfaCodeAsync(string phoneNumber, string code);
    }
}