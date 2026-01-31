using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackEnd.Controllers;

[ApiController]
[Authorize] // must be authorized to read the current user from the JWT
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<MeDto> Me()
    {
        // Most JWTs put the user id in "sub". Some identity providers use NameIdentifier.
        var userId =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Email claim is often "email". Some token issuers use the standard email claim type.
        var email =
            User.FindFirstValue(JwtRegisteredClaimNames.Email) ??
            User.FindFirstValue("email") ??
            User.FindFirstValue(ClaimTypes.Email);

        // FIX: you were using JwtRegisteredClaimNames.Nickname (a constant string) instead of reading a claim value.
        // Also, many providers use "name" / "preferred_username" for display name.
        var displayName =
            User.FindFirstValue(JwtRegisteredClaimNames.PreferredUsername) ??
            User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ??
            User.FindFirstValue("preferred_username") ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirstValue("name") ??
            email ??
            userId ??
            "(unknown)";

        return Ok(new MeDto(userId, email, displayName));
    }

    public sealed record MeDto(string? UserId, string? Email, string DisplayName);
}