using Microsoft.AspNetCore.Mvc;
using Backend.Interfaces;
using Backend.Requests;
using Backend.Responses;
using Microsoft.AspNetCore.Authorization;
using Backend.Entities;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ITokenService tokenService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
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
            _logger.LogInformation("Login request started for {Email}", loginRequest.Email);
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

            _logger.LogInformation("Login successful for {Email}", loginRequest.Email);
            return Ok(loginResponse);
        }

        [Backend.Helpers.Authorize(Role.Admin, Role.User)]
        [HttpGet("all")]
        [ProducesResponseType(typeof(List<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("GetAllUsers request started. UserID={UserID}", UserID);
            if (user == null || user.Role != Role.Admin)
            {
                return Forbid();
            }

            var users = await _userService.GetAllUsersAsync();
            _logger.LogInformation("GetAllUsers successful. Count={Count}", users?.Count ?? 0);
            return Ok(users);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshTokenFromCookie()
        {
            _logger.LogInformation("RefreshToken request started.");
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

            _logger.LogInformation("Refresh token successful.");
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
            _logger.LogInformation("ForgotPassword request started for {Email}", request.Email);
            var origin = Request.Headers["Origin"].ToString();
            var response = await _userService.ForgotPassword(request, origin);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }

            _logger.LogInformation("ForgotPassword initiated successfully for {Email}", request.Email);
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
            _logger.LogInformation("ResetPassword request started.");
            var response = await _userService.ResetPassword(request);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }

            _logger.LogInformation("ResetPassword successful.");
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
            _logger.LogInformation("Register request started for {Email}", registerRequest.Email);
            var origin = Request.Headers["Origin"].ToString();
            var registerResponse = await _userService.RegisterAsync(registerRequest, origin);

            if (!registerResponse.Success)
            {
                return UnprocessableEntity(registerResponse);
            }

            _logger.LogInformation("Register successful for {Email}", registerResponse.Email);
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
            _logger.LogInformation("Logout request started. UserID={UserID}", UserID);
            var logoutResponse = await _userService.LogoutAsync(UserID);

            if (!logoutResponse.Success)
            {
                return UnprocessableEntity(logoutResponse);
            }

            Response.Cookies.Delete("refreshToken");

            _logger.LogInformation("Logout successful for UserID={UserID}", UserID);
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
            _logger.LogInformation("Info request started. UserID={UserID}", UserID);
            var userResponse = await _userService.GetInfoAsync(UserID);

            if (!userResponse.Success)
            {
                return UnprocessableEntity(userResponse);
            }

            _logger.LogInformation("Info successful for UserID={UserID}", UserID);
            return Ok(userResponse);
        }

        /// <summary>
        /// Confirms a user's email address
        /// </summary>
        [HttpPost("confirm_email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] VerifyEmailRequest request)
        {
            _logger.LogInformation("ConfirmEmail request started.");
            var response = await _userService.VerifyEmail(request);
            
            if (!response.Success)
            {
                return BadRequest(new { message = response.Error, errorCode = response.ErrorCode });
            }

            _logger.LogInformation("ConfirmEmail successful for Token={Token}", request.Token);
            return Ok(new { message = response.Message });
        }




        [HttpPost]
        [Route("verify-mfa")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyMfa(VerifyMfaRequest request)
        {
            _logger.LogInformation("VerifyMfa request started for {Email}", request.Email);
            var response = await _userService.VerifyMfaAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                SetTokenCookie(response.RefreshToken);
            }

            _logger.LogInformation("VerifyMfa successful.");
            return Ok(response);
        }

        [Backend.Helpers.Authorize]
        [HttpPost]
        [Route("enable-mfa")]
        [ProducesResponseType(typeof(MfaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnableMfa(EnableMfaRequest request)
        {
            _logger.LogInformation("EnableMfa request started. UserID={UserID}", UserID);
            var response = await _userService.EnableMfaAsync(UserID, request);
            if (response.Success)
                _logger.LogInformation("EnableMfa successful for UserID={UserID}", UserID);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Backend.Helpers.Authorize]
        [HttpPost]
        [Route("disable-mfa")]
        [ProducesResponseType(typeof(MfaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DisableMfa()
        {
            _logger.LogInformation("DisableMfa request started. UserID={UserID}", UserID);
            var response = await _userService.DisableMfaAsync(UserID);
            if (response.Success)
                _logger.LogInformation("DisableMfa successful for UserID={UserID}", UserID);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
