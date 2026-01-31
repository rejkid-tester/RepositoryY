using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Backend;
using Backend.Entities;
using NLog;

namespace Backend.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IList<Role> _roles;

        public AuthorizeAttribute(params Role[] roles)
        {
            _roles = roles?.ToList() ?? new List<Role>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authorization if endpoint allows anonymous
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                Logger.Debug("Authorization skipped due to AllowAnonymous.");
                return;
            }

            var httpContext = context.HttpContext;

            // Try to get the application user placed into HttpContext.Items by the JwtMiddleware
            var user = httpContext.Items["User"] as User;

            // If the middleware didn't populate the User object, fall back to ClaimsPrincipal
            if (user == null)
            {
                var principal = httpContext.User;
                if (principal?.Identity?.IsAuthenticated == true)
                {
                    foreach (var claim in principal.Claims)
                    {
                        Logger.Info("Claim {Type} = {Value}", claim.Type, claim.Value);
                    }
                    var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                    if (!string.IsNullOrEmpty(roleClaim) && Enum.TryParse<Role>(roleClaim, true, out var parsedRole))
                    {
                        user = new User { Role = parsedRole };
                    }
                }
            }

            // If no user (unauthenticated) -> 401
            if (user == null)
            {
                Logger.Warn("Unauthorized request. No user context present.");
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            // If user is authenticated but role is not permitted -> 403
            if (_roles.Any() && !_roles.Contains(user.Role))
            {
                Logger.Warn("Forbidden request. Required roles: {Roles}. Actual role: {Role}", _roles, user.Role);
                context.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
            }
        }
    }
}