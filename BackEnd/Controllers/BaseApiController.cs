using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Backend;
using Backend.Entities;

namespace Backend.Controllers
{
    public class BaseApiController : ControllerBase
    {
        public User? user => HttpContext.Items["User"] as User;

        protected int UserID
        {
            get
            {
                var idStr = FindClaim(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) return id;
                throw new InvalidOperationException("Authenticated user id claim is missing or invalid.");
            }
        }

        protected string? FindClaim(string claimName)
        {
            var attachedUser = HttpContext?.Items["User"] as User;
            if (attachedUser is not null)
            {
                if (string.Equals(claimName, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claimName, "sub", StringComparison.OrdinalIgnoreCase))
                {
                    return attachedUser.Id.ToString();
                }
            }
            return null;
        }

        protected void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(14),
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}

