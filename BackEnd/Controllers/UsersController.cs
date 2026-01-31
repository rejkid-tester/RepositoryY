using Microsoft.AspNetCore.Mvc;
using Backend.Interfaces;
using Backend.Requests;
using Backend.Responses;
using Microsoft.AspNetCore.Authorization;
using Backend.Entities;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public UsersController(IUserService userService, ITokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Authenticates a user and returns access and refresh tokens
        /// </summary>
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new TokenResponse
                {
                    Error = "Missing login details",
                    ErrorCode = "L01"
                });
            }

            var loginResponse = await _userService.LoginAsync(loginRequest);

             if (!loginResponse.Success)
            {
                return Unauthorized(new
                {
                    loginResponse.ErrorCode,
                    loginResponse.Error
                });
            }
            
            if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
            {
                SetTokenCookie(loginResponse.RefreshToken);
            }
            
            return Ok(loginResponse);
        }

        [Backend.Helpers.Authorize(Role.Admin, Role.User)]
        [HttpGet("all")]
        [ProducesResponseType(typeof(List<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers()
        {
            if (user == null || user.Role != Role.Admin)
            {
                return Forbid();
            }

            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshTokenFromCookie()
        {
            var origin = Request.Headers["Origin"].ToString();
            var refreshToken = Request.Cookies["refreshToken"];
            var response = await _tokenService.RefreshTokenAsync(refreshToken, origin);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                SetTokenCookie(response.RefreshToken);
            }

            return Ok(response);
        }

        /// <summary>
        /// Initiates password reset process
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var origin = Request.Headers["Origin"].ToString();
            var response = await _userService.ForgotPassword(request, origin);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }

            return Ok(new { message = response.Message });
        }

        /// <summary>
        /// Resets user password with token
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = await _userService.ResetPassword(request);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }

            return Ok(new { message = response.Message });
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            var origin = Request.Headers["Origin"].ToString();
            var registerResponse = await _userService.RegisterAsync(registerRequest, origin);

            if (!registerResponse.Success)
            {
                return UnprocessableEntity(registerResponse);
            }

            return Ok(registerResponse.Email);
        }

        /// <summary>
        /// Logs out the current user and invalidates their refresh token
        /// </summary>
        [Backend.Helpers.Authorize]
        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Logout()
        {
            var logoutResponse = await _userService.LogoutAsync(UserID);

            if (!logoutResponse.Success)
            {
                return UnprocessableEntity(logoutResponse);
            }

            Response.Cookies.Delete("refreshToken");
            
            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Gets information about the currently authenticated user
        /// </summary>
        [Backend.Helpers.Authorize]
        [HttpGet]
        [Route("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Info()
        {
            var userResponse = await _userService.GetInfoAsync(UserID);

            if (!userResponse.Success)
            {
                return UnprocessableEntity(userResponse);
            }

            return Ok(userResponse);
        }

        /// <summary>
        /// Confirms a user's email address
        /// </summary>
        [HttpPost("confirm_email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] VerifyEmailRequest request)
        {
            var response = await _userService.VerifyEmail(request);
            
            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }
            
            return Ok(new { message = response.Message });
        }




        [HttpPost]
        [Route("verify-mfa")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyMfa(VerifyMfaRequest request)
        {
            var response = await _userService.VerifyMfaAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                SetTokenCookie(response.RefreshToken);
            }

            return Ok(response);
        }

        [Backend.Helpers.Authorize]
        [HttpPost]
        [Route("enable-mfa")]
        [ProducesResponseType(typeof(MfaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnableMfa(EnableMfaRequest request)
        {
            var response = await _userService.EnableMfaAsync(UserID, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Backend.Helpers.Authorize]
        [HttpPost]
        [Route("disable-mfa")]
        [ProducesResponseType(typeof(MfaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DisableMfa()
        {
            var response = await _userService.DisableMfaAsync(UserID);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
