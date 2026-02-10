using Microsoft.EntityFrameworkCore;
using System;
using System.Web;
using Backend.Helpers;
using Backend.Interfaces;
using Backend.Requests;
using Backend.Responses;
using Backend.Services;
using Backend.Entities;
using NLog;
using BackEnd.Interfaces;

namespace Backend.Services
{
    public class UserService : IUserService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly UserDbContext tasksDbContext;
        private readonly ITokenService tokenService;
        private readonly IEmailService emailService;
        private readonly ISmsService _smsService;

        public UserService(UserDbContext tasksDbContext, ITokenService tokenService, 
                           IEmailService emailService, ISmsService smsService)
        {
            this.tasksDbContext = tasksDbContext;
            this.tokenService = tokenService;
            this.emailService = emailService;
            _smsService = smsService;
        }

        public async Task<UserResponse> GetInfoAsync(int userId)
        {
            logger.Info("GetInfoAsync: Start. userId={UserId}", userId);
            var user = await tasksDbContext.Users.FindAsync(userId);

            if (user == null)
            {
                logger.Info("GetInfoAsync: User not found. userId={UserId}", userId);
                return new UserResponse
                {
                    Success = false,
                    Error = "No user found",
                    ErrorCode = "I001"
                };
            }

            logger.Info("GetInfoAsync: Success. userId={UserId}", userId);
            return new UserResponse
            {
                Success = true,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreationDate = user.Created,
                MfaEnabled = user.MfaEnabled,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            logger.Info("GetAllUsersAsync: Start");
            return await tasksDbContext.Users
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest loginRequest)
        {
            logger.Info("LoginAsync: Start. email={Email}", loginRequest.Email);
            var user = tasksDbContext.Users.SingleOrDefault(user => 
                user.Active && user.Email == loginRequest.Email);

            if (user == null)
            {
                logger.Info("LoginAsync: Email not found or inactive. email={Email}", loginRequest.Email);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Email not found",
                    ErrorCode = "L02"
                };
            }
            logger.Info("LoginAsync: User found. userId={UserId} email={Email}", user.Id, user.Email);
            
            var passwordHash = PasswordHelper.HashUsingPbkdf2(
                loginRequest.Password, 
                Convert.FromBase64String(user.PasswordSalt));

            if (user.Password != passwordHash)
            {
                logger.Info("LoginAsync: Invalid password. userId={UserId} email={Email}", user.Id, user.Email);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Invalid Password",
                    ErrorCode = "L03"
                };
            }
            logger.Info("LoginAsync: Password OK. userId={UserId}", user.Id);

            // Check if MFA is enabled
            if (user.MfaEnabled && !string.IsNullOrEmpty(user.PhoneNumber))
            {
                logger.Info("LoginAsync: MFA required. userId={UserId}", user.Id);
                // Generate and send MFA code
                var mfaCode = GenerateMfaCode();
                user.MfaCode = mfaCode;
                user.MfaCodeExpires = DateTime.UtcNow.AddMinutes(5);
                tasksDbContext.Users.Update(user);
                await tasksDbContext.SaveChangesAsync();
                logger.Info("LoginAsync: MFA code generated & persisted. userId={UserId} expires={Expires}", user.Id, user.MfaCodeExpires);
                
                await _smsService.SendMfaCodeAsync(user.PhoneNumber, mfaCode);
                logger.Info("LoginAsync: MFA SMS send attempted. userId={UserId}", user.Id);
                
                return new TokenResponse
                {
                    Success = true,
                    MfaRequired = true,
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    SecondName = user.LastName,
                    Email = user.Email
                };
            }

            logger.Info("LoginAsync: MFA not required. userId={UserId}", user.Id);

            var token = await tokenService.GenerateTokensAsync(user.Id);
            logger.Info("LoginAsync: Tokens generated. userId={UserId}", user.Id);

            return new TokenResponse
            {
                Success = true,
                AccessToken = token!.Item1,
                RefreshToken = token.Item2,
                UserId = user.Id,
                FirstName = user.FirstName,
                SecondName = user.LastName,
                Email = user.Email
            };
        }
        
        public async Task<TokenResponse> VerifyMfaAsync(VerifyMfaRequest request)
        {
            logger.Info("VerifyMfaAsync: Start. email={Email}", request.Email);
            var user = await tasksDbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Active);

            if (user == null || !user.MfaEnabled)
            {
                logger.Info("VerifyMfaAsync: Invalid request. email={Email} userFound={UserFound} mfaEnabled={MfaEnabled}", request.Email, user != null, user?.MfaEnabled);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Invalid request",
                    ErrorCode = "MFA01"
                };
            }
            logger.Info("VerifyMfaAsync: User found. userId={UserId}", user.Id);

            if (user.MfaCode != request.Code)
            {
                logger.Info("VerifyMfaAsync: Invalid code. userId={UserId}", user.Id);
                return new TokenResponse
                {
                    Success = false,
                    Error = "Invalid MFA code",
                    ErrorCode = "MFA02"
                };
            }
            logger.Info("VerifyMfaAsync: Code match. userId={UserId}", user.Id);

            if (user.MfaCodeExpires.HasValue && user.MfaCodeExpires.Value < DateTime.UtcNow)
            {
                logger.Info("VerifyMfaAsync: Code expired. userId={UserId} expires={Expires}", user.Id, user.MfaCodeExpires);
                return new TokenResponse
                {
                    Success = false,
                    Error = "MFA code expired",
                    ErrorCode = "MFA03"
                };
            }
            logger.Info("VerifyMfaAsync: Code not expired. userId={UserId}", user.Id);

            // Clear MFA code
            user.MfaCode = null;
            user.MfaCodeExpires = null;
            tasksDbContext.Users.Update(user);
            await tasksDbContext.SaveChangesAsync();
            logger.Info("VerifyMfaAsync: MFA code cleared. userId={UserId}", user.Id);

            var token = await tokenService.GenerateTokensAsync(user.Id);
            logger.Info("VerifyMfaAsync: Tokens generated. userId={UserId}", user.Id);

            return new TokenResponse
            {
                Success = true,
                AccessToken = token!.Item1,
                RefreshToken = token.Item2,
                UserId = user.Id,
                FirstName = user.FirstName,
                SecondName = user.LastName,
                Email = user.Email
            };
        }
        
        public async Task<MfaResponse> EnableMfaAsync(int userId, EnableMfaRequest request)
        {
            logger.Info("EnableMfaAsync: Start. userId={UserId}", userId);
            var user = await tasksDbContext.Users.FindAsync(userId);

            if (user == null)
            {
                logger.Info("EnableMfaAsync: User not found. userId={UserId}", userId);
                return new MfaResponse
                {
                    Success = false,
                    Error = "User not found",
                    ErrorCode = "MFA04"
                };
            }

            user.PhoneNumber = request.PhoneNumber;
            user.MfaEnabled = true;
            tasksDbContext.Users.Update(user);
            await tasksDbContext.SaveChangesAsync();
            logger.Info("EnableMfaAsync: Success. userId={UserId} phoneNumberSet={PhoneNumberSet}", userId, !string.IsNullOrWhiteSpace(user.PhoneNumber));

            return new MfaResponse
            {
                Success = true,
                Message = "MFA enabled successfully"
            };
        }
        
        public async Task<MfaResponse> DisableMfaAsync(int userId)
        {
            logger.Info("DisableMfaAsync: Start. userId={UserId}", userId);
            var user = await tasksDbContext.Users.FindAsync(userId);

            if (user == null)
            {
                logger.Info("DisableMfaAsync: User not found. userId={UserId}", userId);
                return new MfaResponse
                {
                    Success = false,
                    Error = "User not found",
                    ErrorCode = "MFA05"
                };
            }

            user.MfaEnabled = false;
            user.MfaCode = null;
            user.MfaCodeExpires = null;
            tasksDbContext.Users.Update(user);
            await tasksDbContext.SaveChangesAsync();
            logger.Info("DisableMfaAsync: Success. userId={UserId}", userId);

            return new MfaResponse
            {
                Success = true,
                Message = "MFA disabled successfully"
            };
        }
        
        public async Task<LogoutResponse> LogoutAsync(int userId)
        {
            logger.Info("LogoutAsync: Start. userId={UserId}", userId);
            var refreshToken = await tasksDbContext.RefreshTokens.FirstOrDefaultAsync(o => o.UserId == userId);

            if (refreshToken == null)
            {
                logger.Info("LogoutAsync: No refresh token found. userId={UserId}", userId);
                return new LogoutResponse { Success = true };
            }

            tasksDbContext.RefreshTokens.Remove(refreshToken);
            await tasksDbContext.SaveChangesAsync();

            logger.Info("LogoutAsync: Refresh token removed. userId={UserId}", userId);

            return new LogoutResponse { Success = true };
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest, string origin)
        {
            // Validate password confirmation
            if (registerRequest.Password != registerRequest.ConfirmPassword)
            {
                logger.Warn("RegisterAsync: Passwords do not match. email={Email}", registerRequest.Email);
                return new RegisterResponse
                {
                    Success = false,
                    Error = "Passwords do not match",
                    ErrorCode = "S01"
                };
            }

            // Validate MFA setup if requested
            if (registerRequest.EnableMfa)
            {
                if (string.IsNullOrEmpty(registerRequest.PhoneNumber))
                {
                    logger.Warn("RegisterAsync: MFA requested but phone number missing. email={Email}", registerRequest.Email);
                    return new RegisterResponse
                    {
                        Success = false,
                        Error = "Phone number is required to enable MFA",
                        ErrorCode = "S04"
                    };
                }

                // Validate E.164 phone format
                if (!registerRequest.PhoneNumber.StartsWith("+"))
                {
                    logger.Warn("RegisterAsync: Invalid phone format for MFA. email={Email}", registerRequest.Email);
                    return new RegisterResponse
                    {
                        Success = false,
                        Error = "Phone number must be in E.164 format (e.g., +61412345678)",
                        ErrorCode = "S05"
                    };
                }
            }

            // Check if user already exists
            var existingUser = await tasksDbContext.Users
                .FirstOrDefaultAsync(u => u.Email == registerRequest.Email);

            if (existingUser != null)
            {
                logger.Info("RegisterAsync: User already exists. email={Email}", registerRequest.Email);
                await SendAlreadyRegisteredEmail(registerRequest.Email, origin);
                return new RegisterResponse
                {
                    Success = true,
                    Error = "User already exists with the same email",
                    ErrorCode = "S02"
                };
            }

            // Hash password
            var salt = PasswordHelper.GetSecureSalt();
            var passwordHash = PasswordHelper.HashUsingPbkdf2(registerRequest.Password, salt);

            var isFirstAccount = tasksDbContext.Users.Count() == 0;
            // Create new user
            var user = new User
            {
                Email = registerRequest.Email,
                Password = passwordHash,
                PasswordSalt = Convert.ToBase64String(salt),
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                DOB = registerRequest.Dob.ToString(ConstantsDefined.DateFormat),
                VerificationToken = TokenHelper.randomTokenString(),
                Ts = registerRequest.Ts,
                Created = DateTime.UtcNow,
                Active = true,
                Role = isFirstAccount ? Role.Admin : Role.User,
                // MFA setup
                PhoneNumber = registerRequest.PhoneNumber,
                MfaEnabled = registerRequest.EnableMfa,
            };

            tasksDbContext.Users.Add(user);
            var saveResponse = await tasksDbContext.SaveChangesAsync();

            if (saveResponse >= 0)
            {
                // Send verification email
                await SendVerificationEmail(user, origin);

                return new RegisterResponse
                {
                    Success = true,
                    Email = user.Email
                };
            }

            logger.Error("RegisterAsync: Failed to persist new user. email={Email} saveResponse={SaveResponse}", registerRequest.Email, saveResponse);
            return new RegisterResponse
            {
                Success = false,
                Error = "Unable to create user",
                ErrorCode = "S03"
            };
        }

        public async Task<VerifyEmailResponse> VerifyEmail(VerifyEmailRequest request)
        {
            var user = await tasksDbContext.Users
                .FirstOrDefaultAsync(u => u.VerificationToken == request.Token);

            if (user == null)
            {
                logger.Warn("VerifyEmail: Invalid verification token.");
                return new VerifyEmailResponse
                {
                    Success = false,
                    Error = "Invalid verification token",
                    ErrorCode = "V01"
                };
            }

            // Optional: Verify DOB if provided for additional security
            if (!string.IsNullOrEmpty(request.Dob) && user.DOB != request.Dob)
            {
                logger.Warn("VerifyEmail: DOB mismatch. userId={UserId}", user.Id);
                return new VerifyEmailResponse
                {
                    Success = false,
                    Error = "Verification failed - invalid credentials",
                    ErrorCode = "V02"
                };
            }

            // Check if already verified
            if (user.Verified.HasValue)
            {
                logger.Info("VerifyEmail: Email already verified. userId={UserId}", user.Id);
                return new VerifyEmailResponse
                {
                    Success = false,
                    Error = "Email already verified",
                    ErrorCode = "V03"
                };
            }

            // Mark as verified
            user.Verified = DateTime.UtcNow;
            user.VerificationToken = null;

            await tasksDbContext.SaveChangesAsync();

            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email verified successfully! You can now log in."
            };
        }

        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, string origin)
        {
            var user = await tasksDbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Always return success to prevent email enumeration
            if (user == null)
            {
                logger.Info("ForgotPassword: User not found (enumeration-safe). email={Email}", request.Email);
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If the user exists, a password reset email has been sent."
                };
            }

            // Optional: Verify DOB if provided
            if (!string.IsNullOrEmpty(request.Dob) && user.DOB != request.Dob)
            {
                logger.Info("ForgotPassword: DOB mismatch (enumeration-safe). email={Email}", request.Email);
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If the user exists, a password reset email has been sent."
                };
            }

            // Generate reset token
            user.ResetToken = TokenHelper.randomTokenString();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await tasksDbContext.SaveChangesAsync();

            // Send reset email
            await SendPasswordResetEmail(user, origin);

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If the account exists, a password reset email has been sent."
            };
        }

        public async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request)
        {
            var user = await tasksDbContext.Users
                .FirstOrDefaultAsync(u => u.ResetToken == request.Token);

            if (user == null)
            {
                logger.Warn("ResetPassword: Invalid or expired reset token.");
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Invalid or expired reset token",
                    ErrorCode = "R01"
                };
            }

            // Check if token has expired
            if (user.ResetTokenExpires.HasValue && user.ResetTokenExpires.Value < DateTime.UtcNow)
            {
                logger.Warn("ResetPassword: Reset token expired. userId={UserId}", user.Id);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Reset token has expired",
                    ErrorCode = "R02"
                };
            }

            // Validate password match
            if (request.Password != request.ConfirmPassword)
            {
                logger.Warn("ResetPassword: Passwords do not match. userId={UserId}", user.Id);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Error = "Passwords do not match",
                    ErrorCode = "R03"
                };
            }

            // Hash new password
            var salt = PasswordHelper.GetSecureSalt();
            var passwordHash = PasswordHelper.HashUsingPbkdf2(request.Password, salt);

            // Update user password
            user.Password = passwordHash;
            user.PasswordSalt = Convert.ToBase64String(salt);
            user.ResetToken = null;
            user.ResetTokenExpires = null;

            await tasksDbContext.SaveChangesAsync();

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Password reset successful. You can now log in with your new password."
            };
        }

        private async Task SendVerificationEmail(User user, string origin)
        {
            var normalizedOrigin = string.IsNullOrWhiteSpace(origin)
                ? "http://localhost:4200"
                : origin.TrimEnd('/');
            
            // Ensure VerificationToken is not null
            if (string.IsNullOrEmpty(user.VerificationToken))
            {
                return;
            }
            
            var verifyUrl = $"{normalizedOrigin}/account/verify-email?token={Uri.EscapeDataString(user.VerificationToken)}&DOB={Uri.EscapeDataString(user.DOB ?? string.Empty)}";

            await EmailHelper.SendVerificationEmailAsync(emailService, user.Email, user.FirstName, verifyUrl);
        }

        private async Task SendPasswordResetEmail(User user, string origin)
        {
            var normalizedOrigin = origin.TrimEnd('/');
            
            // Ensure ResetToken is not null
            if (string.IsNullOrEmpty(user.ResetToken))
            {
                return;
            }
            
            var resetUrl = $"{normalizedOrigin}/account/reset-password?token={Uri.EscapeDataString(user.ResetToken)}&DOB={System.Web.HttpUtility.UrlEncode(user.DOB)}";

            await EmailHelper.SendPasswordResetEmailAsync(emailService, user.Email, user.FirstName, resetUrl);
        }

        private async Task SendAlreadyRegisteredEmail(string email, string origin)
        {
            var normalizedOrigin = origin.TrimEnd('/');
            var loginUrl = $"{normalizedOrigin}/account/login";
            var resetUrl = $"{normalizedOrigin}/account/forgot-password";

            await EmailHelper.SendAlreadyRegisteredEmailAsync(emailService, email, loginUrl, resetUrl);
        }
        
        private static string GenerateMfaCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}

