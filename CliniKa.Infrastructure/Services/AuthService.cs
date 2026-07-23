using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CliniKa.Application;
using CliniKa.Application.Constants;
using System.Text;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Authentication;
using CliniKa.Infrastructure.Persistence.Identity;

namespace CliniKa.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtProvider _jwtProvider;
        private readonly IValidator<ConfirmEmailDto> _confirmEmailValidator;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<ForgetPasswordRequest> _forgetPasswordValidator;
        private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtProvider jwtProvider,
            IValidator<ConfirmEmailDto> confirmEmailValidator,
            IValidator<LoginRequest> loginValidator,
            IValidator<RegisterRequest> registerValidator,
            IValidator<ForgetPasswordRequest> forgetPasswordValidator,
            IValidator<ResetPasswordRequest> resetPasswordValidator,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtProvider = jwtProvider;
            _confirmEmailValidator = confirmEmailValidator;
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
            _forgetPasswordValidator = forgetPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<GeneralResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
        {
            var validationResult = await _confirmEmailValidator.ValidateAsync(confirmEmailDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult.ValidationFailure(errors);
            }

            var user = await _userManager.FindByIdAsync(confirmEmailDto.UserId);
            if (user == null)
            {
                return GeneralResult.NotFound("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, confirmEmailDto.Token);
            if (result.Succeeded)
            {
                return GeneralResult.Success("Email confirmed successfully.");
            }

            return GeneralResult.Failure("Email confirmation failed.");
        }

        public async Task<GeneralResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        {

            var validationResult = await _loginValidator.ValidateAsync(loginRequest);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<LoginResponse>.ValidationFailure(errors);
            }


            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user is null)
                return GeneralResult<LoginResponse>.Failure("Invalid email or password.");

            if (!user.IsActive)
                return GeneralResult<LoginResponse>.Failure("User account is deactivated.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                if (result.IsNotAllowed)
                    return GeneralResult<LoginResponse>.Failure("Email is not confirmed.");

                //if (result.IsLockedOut)
                //    return GeneralResult<LoginResponse>.Failure("User account is locked out.");

                return GeneralResult<LoginResponse>.Failure("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);


            var (token, expireIn) = _jwtProvider.GenerateToken(user.Id, user.FirstName, user.LastName, user.Email!, roles);

            var response = new LoginResponse(
                Id: user.Id,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Email: user.Email!,
                Token: token,
                ExpiresIn: expireIn);

            return GeneralResult<LoginResponse>.Success(response, "Login successful.");
        }

        public async Task<GeneralResult<RegisterResponse>> RegisterAsync(RegisterRequest registerRequest)
        {
            var validationResult = await _registerValidator.ValidateAsync(registerRequest);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<RegisterResponse>.ValidationFailure(errors);
            }

            var existingUser = await _userManager.FindByEmailAsync(registerRequest.Email);
            if (existingUser != null)
            {
                return GeneralResult<RegisterResponse>.Failure("Email is already registered.");
            }

            var user = new ApplicationUser
            {
                UserName = registerRequest.Email,
                Email = registerRequest.Email,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                PhoneNumber = registerRequest.PhoneNumber,
                IsActive = true
            };
            var createResult = await _userManager.CreateAsync(user, registerRequest.Password);

            if (!createResult.Succeeded)
            {
                var message = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return GeneralResult<RegisterResponse>.Failure($"User registration failed: {message}");
            }

             var createRoleResult =   await _userManager.AddToRoleAsync(user, RoleConstants.Patient);

            if(!createRoleResult.Succeeded)
            {
                var message = string.Join(", ", createRoleResult.Errors.Select(e => e.Description));
                return GeneralResult<RegisterResponse>.Failure($"User registration failed: {message}");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var baseUrl = _configuration["App:BaseUrl"];
            //var confirmationLink =
            //    $"{baseUrl}/api/auth/confirm-email?UserId={Uri.EscapeDataString(user.Id)}&Token={Uri.EscapeDataString(token)}";


            //await _emailSender.SendAsync(
            //    user.Email!,
            //    "Confirm your email",
            //    $"""
            //    <p>Hello {user.FirstName},</p>
            //    <p>Please confirm your email by clicking this link:</p>
            //    <p><a href="{confirmationLink}">Confirm Email</a></p>
            //    """);

            var response = new RegisterResponse(
                Id: user.Id,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Email: user.Email!,
                PhoneNumber: user.PhoneNumber!
                );

            return GeneralResult<RegisterResponse>.Success(response, "User registered successfully.");



        }

        public async Task<GeneralResult> ForgetPasswordAsync(ForgetPasswordRequest forgetPasswordRequest)
        {
            var validationResult = await _forgetPasswordValidator.ValidateAsync(forgetPasswordRequest);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult.ValidationFailure(errors);
            }

            var user = await _userManager.FindByEmailAsync(forgetPasswordRequest.Email);
            if (user == null)
            {
                return GeneralResult.Success("If your email is registered, you will receive an OTP code.");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            _logger.LogInformation("Reset Password {Token}", code);

            var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin;
            var actionUrl = $"{origin}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(code)}";

            var forgetPassEmailBody = $"""
                <p>Hello {user.FirstName},</p>
                <p>You requested a password reset. Please click the link below to reset your password:</p>
                <p><a href="{actionUrl}">Reset Password</a></p>
                <p>If you did not request this, please ignore this email.</p>
                """;

            await _emailSender.SendAsync(
                user.Email!,
                "clinika",
                forgetPassEmailBody);

            return GeneralResult.Success("If your email is registered, you will receive an OTP code.");
        }

        public async Task<GeneralResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(resetPasswordRequest);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult.ValidationFailure(errors);
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordRequest.Email);
            if (user == null)
            {
                return GeneralResult.Failure("Invalid email or code.");
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordRequest.Code));
            var result = await _userManager.ResetPasswordAsync(user, decodedCode, resetPasswordRequest.NewPassword);
            if (!result.Succeeded)
            {
                var message = string.Join(", ", result.Errors.Select(e => e.Description));
                return GeneralResult.Failure($"Password reset failed: {message}");
            }

            return GeneralResult.Success("Password has been reset successfully.");
        }

        public async Task ForceResetPass()
        {
            var user = await _userManager.FindByEmailAsync("abdelrahamnsaied7crepee@gmail.com");


            var token = await _userManager.GeneratePasswordResetTokenAsync(user!);

            await _userManager.ResetPasswordAsync(user!, token, "7crepeeP@a$s$ord");


        }
    }
}
