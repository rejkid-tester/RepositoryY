using Backend.Entities;
using Backend.Requests;
using Backend.Responses;

namespace Backend.Interfaces
{
    public interface ITokenService
    {
        Task<Tuple<string, string>?> GenerateTokensAsync(int userId);
        Task<bool> RemoveRefreshTokenAsync(User user);

        Task<TokenResponse> RefreshTokenAsync(string? refreshToken, string origin);
    }
}
