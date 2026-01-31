using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Backend.Entities;

namespace Backend.Helpers
{
    public static class TokenHelper
    {
        public static string Issuer { get; private set; } = "http://codingsonata.com";
        public static string Audience { get; private set; } = "http://codingsonata.com";
        public static string Secret { get; private set; } = "";

        // Call this once at startup (in Program.cs) to set values from config
        public static void Configure(IConfiguration config)
        {
            Issuer =
                Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? config["AppSettings:JWT_ISSUER"]
                ?? config["Jwt:Issuer"]
                ?? Issuer;

            Audience =
                Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? config["AppSettings:JWT_AUDIENCE"]
                ?? config["Jwt:Audience"]
                ?? Audience;

            Secret =
                Environment.GetEnvironmentVariable("ACCESS_TOKEN_KEY")
                ?? config["AppSettings:ACCESS_TOKEN_KEY"]
                ?? Secret;
        }

        public static byte[] GetAccessTokenKeyBytesOrThrow(string? accessTokenKey)
        {
            if (string.IsNullOrWhiteSpace(accessTokenKey))
                throw new InvalidOperationException("Missing required ACCESS_TOKEN_KEY.");

            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(accessTokenKey);
            }
            catch
            {
                keyBytes = Encoding.UTF8.GetBytes(accessTokenKey);
            }

            if (keyBytes.Length < 32)
                throw new InvalidOperationException("JWT secret key is too short. Provide at least 256 bits (32 bytes).");

            return keyBytes;
        }

        public static TokenValidationParameters CreateTokenValidationParameters(byte[] keyBytes)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };
        }

        public static async Task<string> GenerateAccessToken(User user)
        {
            var userId = user.Id;

            byte[] key;
            try
            {
                key = Convert.FromBase64String(Secret);
            }
            catch
            {
                key = Encoding.UTF8.GetBytes(Secret);
            }

            if (key.Length < 32)
                throw new InvalidOperationException("JWT secret key is too short. Provide at least 256 bits (32 bytes).");

            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.PreferredUsername, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            };

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = Issuer,
                Audience = Audience,
                NotBefore = now.UtcDateTime,
                IssuedAt = now.UtcDateTime,
                Expires = now.AddMinutes(15).UtcDateTime,
                SigningCredentials = signingCredentials
            };

            var handler = new JsonWebTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false
            };

            var token = handler.CreateToken(tokenDescriptor);
            return await Task.FromResult(token);
        }

        public static async Task<string> GenerateRefreshToken()
        {
            var secureRandomBytes = new byte[32];

            using var randomNumberGenerator = RandomNumberGenerator.Create();
            await Task.Run(() => randomNumberGenerator.GetBytes(secureRandomBytes));

            var refreshToken = Convert.ToBase64String(secureRandomBytes);
            return refreshToken;
        }

        public static string randomTokenString()
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[40];

            rng.GetBytes(randomBytes);

            return BitConverter.ToString(randomBytes).Replace("-", "");
        }
    }
}
