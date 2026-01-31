using Microsoft.EntityFrameworkCore;
using Backend.Helpers;
using Backend.Interfaces;
using Backend.Responses;
using Backend.Entities;
using Microsoft.Extensions.Options;
using System.Linq;
using NLog;

namespace Backend.Services
{
    public class TokenService : ITokenService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        private readonly UserDbContext tasksDbContext;
        private readonly AppSettings _appSettings;

        public TokenService(UserDbContext tasksDbContext,
            IOptions<AppSettings> appSettings)
        {
            this.tasksDbContext = tasksDbContext;
            this._appSettings = appSettings.Value;
        }

        public async Task<Tuple<string, string>?> GenerateTokensAsync(int userId)
        {
            logger.Info("GenerateTokensAsync: Start. userId={UserId}", userId);

            var userRecord = await tasksDbContext.Users.Include(o => o.RefreshTokens).FirstOrDefaultAsync(e => e.Id == userId);

            if (userRecord == null)
            {
                logger.Info("GenerateTokensAsync: User not found. userId={UserId}", userId);
                return null;
            }
            logger.Info("GenerateTokensAsync: User found. userId={UserId}", userId);

            var accessToken = await TokenHelper.GenerateAccessToken(userRecord);
            var refreshToken = await TokenHelper.GenerateRefreshToken();
            logger.Info("GenerateTokensAsync: Tokens created in memory. userId={UserId}", userId);

            var salt = PasswordHelper.GetSecureSalt();

            var refreshTokenHashed = PasswordHelper.HashUsingPbkdf2(refreshToken, salt);

            if (userRecord.RefreshTokens != null && userRecord.RefreshTokens.Any())
            {
                logger.Info("GenerateTokensAsync: Existing refresh token(s) found; removing all. userId={UserId} count={Count}", userId, userRecord.RefreshTokens.Count);
                tasksDbContext.RefreshTokens.RemoveRange(userRecord.RefreshTokens);

            }
            tasksDbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = refreshTokenHashed,
                TokenSalt = Convert.ToBase64String(salt),
                Ts = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(14),
                Created = DateTime.UtcNow
            });

            var addedEntries = tasksDbContext.ChangeTracker.Entries<RefreshToken>().Count(e => e.State == EntityState.Added);
            logger.Info("GenerateTokensAsync: RefreshToken entries in Added state before SaveChanges: {Count}. userId={UserId}", addedEntries, userId);
            await tasksDbContext.SaveChangesAsync();
            logger.Info("GenerateTokensAsync: Tokens persisted. userId={UserId}", userId);

            // Return the *raw* refresh token to the client. Only the hash+salt are stored in the DB.
            var token = new Tuple<string, string>(accessToken, refreshToken);

            return token;
        }

        public async Task<bool> RemoveRefreshTokenAsync(User user)
        {
            logger.Info("RemoveRefreshTokenAsync: Start. userId={UserId}", user.Id);
            var userRecord = await tasksDbContext.Users.Include(o => o.RefreshTokens).FirstOrDefaultAsync(e => e.Id == user.Id);

            if (userRecord == null)
            {
                logger.Info("RemoveRefreshTokenAsync: User not found. userId={UserId}", user.Id);
                return false;
            }

            if (userRecord.RefreshTokens != null && userRecord.RefreshTokens.Any())
            {
                tasksDbContext.RefreshTokens.RemoveRange(userRecord.RefreshTokens);
                await tasksDbContext.SaveChangesAsync();
                return true;
            }

            logger.Info("RemoveRefreshTokenAsync: No refresh token to remove. userId={UserId}", user.Id);
            return false;
        }

        public async Task<TokenResponse> RefreshTokenAsync(string? refreshToken, string origin)
        {
            logger.Info("RefreshTokenAsync: Start. origin={Origin}", origin);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                logger.Info("RefreshTokenAsync: Missing refresh token. origin={Origin}", origin);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Missing refresh token",
                    ErrorCode = "invalid_grant"
                };
            }

            // We store refresh tokens as hash+salt. Resolve which stored token this raw token matches.
            // Current schema suggests 1 refresh token per user (TokenService removes existing before adding a new one).
            var candidates = await tasksDbContext.RefreshTokens.AsNoTracking().ToListAsync();
            RefreshToken? matched = null;

            foreach (var candidate in candidates)
            {
                var hash = PasswordHelper.HashUsingPbkdf2(refreshToken, Convert.FromBase64String(candidate.TokenSalt));
                if (hash == candidate.TokenHash)
                {
                    matched = candidate;
                    break;
                }
            }
            if (matched == null)
            {
                logger.Info("RefreshTokenAsync: Invalid refresh token. origin={Origin}", origin);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Invalid refresh token",
                    ErrorCode = "invalid_grant"
                };
            }
            logger.Info("RefreshTokenAsync: Refresh token matched. userId={UserId} origin={Origin}", matched.UserId, origin);

            if (matched.ExpiryDate < DateTime.UtcNow)
            {
                logger.Info("RefreshTokenAsync: Refresh token expired. userId={UserId} expiry={Expiry}", matched.UserId, matched.ExpiryDate);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Refresh token has expired",
                    ErrorCode = "invalid_grant"
                };
            }


            // Rotate refresh token: GenerateTokensAsync removes existing refresh token(s) and adds a new one.
            var tokenPair = await GenerateTokensAsync(matched.UserId);
            if (tokenPair == null)
            {
                logger.Info("RefreshTokenAsync: Could not generate new tokens. userId={UserId}", matched.UserId);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Could not generate new tokens",
                    ErrorCode = "invalid_grant"
                };
            }
            logger.Info("RefreshTokenAsync: Token rotation complete. userId={UserId}", matched.UserId);

            var user = await tasksDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == matched.UserId);

            // Cleanup: remove expired refresh tokens for this user (if any). With current rotation logic,
            // there should typically be only one.
            await RemoveExpiredRefreshTokensAsync(matched.UserId);
            logger.Info("RefreshTokenAsync: Completed. userId={UserId}", matched.UserId);

            return new TokenResponse
            {
                Success = true,
                AccessToken = tokenPair.Item1,
                RefreshToken = tokenPair.Item2,
                UserId = matched.UserId,
                FirstName = user?.FirstName,
                SecondName = user?.LastName,
                Email = user?.Email
            };
        }

        private async Task RemoveExpiredRefreshTokensAsync(int userId)
        {
            logger.Info("RemoveExpiredRefreshTokensAsync: Start. userId={UserId}", userId);
            var cutoff = DateTime.UtcNow.AddDays(-_appSettings.RefreshTokenTTL);
            var expired = await tasksDbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && (rt.ExpiryDate < DateTime.UtcNow || rt.Ts < cutoff))
                .ToListAsync();

            if (expired.Count == 0)
            {
                logger.Info("RemoveExpiredRefreshTokensAsync: None found. userId={UserId}", userId);
                return;
            }

            tasksDbContext.RefreshTokens.RemoveRange(expired);
            await tasksDbContext.SaveChangesAsync();
            logger.Info("RemoveExpiredRefreshTokensAsync: Removed {Count} token(s). userId={UserId}", expired.Count, userId);
        }
    }
}
