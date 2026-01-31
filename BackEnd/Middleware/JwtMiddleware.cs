using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Backend;
using Backend.Helpers;


//using Backend.Helpers;

namespace Backend.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, UserDbContext dataContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await attachUserToContext(context, dataContext, token);
            await _next(context);
        }

        private async Task attachUserToContext(HttpContext context, UserDbContext dataContext, string token)
        {
            try
            {
                var tokenHandler = new JsonWebTokenHandler();

                var accessTokenKey =
                    Environment.GetEnvironmentVariable("ACCESS_TOKEN_KEY")
                    ?? TokenHelper.Secret;

                byte[] keyBytes;
                try
                {
                    keyBytes = TokenHelper.GetAccessTokenKeyBytesOrThrow(accessTokenKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message} Skipping token validation.");
                    return;
                }

                var validationParameters = TokenHelper.CreateTokenValidationParameters(keyBytes);

                var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"Token validated unsuccessfully. Exception: {validationResult.Exception}");
                    return;
                }

                // Try to obtain the subject (user id) from the validated identity first
                string? subject = null;

                // Note: `validationResult.ClaimsIdentity` can be null when using `JsonWebTokenHandler`.
                // The handler may not always populate a ClaimsIdentity for certain token formats or validation modes,
                // so we must guard against null and provide a fallback. We first attempt to read the subject from the
                // ClaimsIdentity (checking `ClaimTypes.NameIdentifier`, `JwtRegisteredClaimNames.Sub`, and "sub"),
                // and if that fails we fall back to extracting the "sub" or NameIdentifier claim directly from the
                // validated `JsonWebToken` (see the fallback logic below).
                if (validationResult.ClaimsIdentity != null)
                {
                    var idClaim = validationResult.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier) ??
                                  validationResult.ClaimsIdentity.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) ??
                                  validationResult.ClaimsIdentity.FindFirst("sub");

                    subject = idClaim?.Value;
                }

                // Fallback: extract from security token if available
                if (string.IsNullOrEmpty(subject) && validationResult.SecurityToken is JsonWebToken jsonWebToken)
                {
                    subject = jsonWebToken.Claims.FirstOrDefault(c => string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase))?.Value
                              ?? jsonWebToken.Claims.FirstOrDefault(c => string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))?.Value;
                }

                if (string.IsNullOrEmpty(subject))
                {
                    Console.WriteLine("Token does not contain a subject claim. Skipping attaching user.");
                    return;
                }

                if (!int.TryParse(subject, out var userId))
                {
                    Console.WriteLine($"Subject claim is not a valid integer: {subject}");
                    return;
                }

                var user = await dataContext.Users.FindAsync(userId);
                if (user != null)
                {
                    context.Items["User"] = user;
                }
                else
                {
                    Console.WriteLine($"User not found for id {userId}");
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"\nToken validation failed: {error.Message}");
            }
        }
    }
}