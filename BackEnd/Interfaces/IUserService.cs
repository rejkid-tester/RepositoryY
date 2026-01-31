using Backend.Requests;
using Backend.Responses;
using Backend.Entities;

namespace Backend.Interfaces
{
    public interface IUserService
    {
        Task<TokenResponse> LoginAsync(LoginRequest loginRequest);
        Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest, string origin);
        Task<LogoutResponse> LogoutAsync(int userId);
        Task<UserResponse> GetInfoAsync(int userId);
        Task<VerifyEmailResponse> VerifyEmail(VerifyEmailRequest request);
        Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, string origin);
        Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request);
        Task<TokenResponse> VerifyMfaAsync(VerifyMfaRequest request);
        Task<MfaResponse> EnableMfaAsync(int userId, EnableMfaRequest request);
        Task<MfaResponse> DisableMfaAsync(int userId);

        Task<List<User>> GetAllUsersAsync();
    }
}
