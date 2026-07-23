using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.User;
using CliniKa.Infrastructure.Persistence.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CliniKa.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IValidator<CreateUserRequest> _createUserValidator;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IValidator<CreateUserRequest> createUserValidator,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _createUserValidator = createUserValidator;
            _logger = logger;
        }

        public async Task<GeneralResult<UserDto>> CreateUserAsync(CreateUserRequest request)
        {
            var validationResult = await _createUserValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<UserDto>.ValidationFailure(errors);
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return GeneralResult<UserDto>.Failure("Email is already registered.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            if (!roleExists)
            {
                return GeneralResult<UserDto>.Failure($"Role '{request.Role}' does not exist.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var message = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return GeneralResult<UserDto>.Failure($"User creation failed: {message}");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!addRoleResult.Succeeded)
            {
                var message = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                return GeneralResult<UserDto>.Failure($"Assigning role failed: {message}");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = new[] { request.Role }
            };

            return GeneralResult<UserDto>.Success(userDto, "User created successfully.");
        }

        public async Task<GeneralResult<IEnumerable<UserDto>>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return GeneralResult<IEnumerable<UserDto>>.Success(userDtos, "Users retrieved successfully.");
        }

        public async Task<GeneralResult<UserDto>> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return GeneralResult<UserDto>.NotFound("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = roles
            };

            return GeneralResult<UserDto>.Success(userDto, "User retrieved successfully.");
        }

        public async Task<GeneralResult> SetUserActiveStatusAsync(string id, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return GeneralResult.NotFound("User not found.");
            }

            if (user.IsActive == isActive)
            {
                var currentStatus = isActive ? "active" : "deactivated";
                return GeneralResult.Success($"User is already {currentStatus}.");
            }

            user.IsActive = isActive;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var message = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return GeneralResult.Failure($"Failed to update user status: {message}");
            }

            var statusMessage = isActive ? "User activated successfully." : "User deactivated successfully.";
            return GeneralResult.Success(statusMessage);
        }

        public Task<GeneralResult> ActivateUserAsync(string id)
        {
            return SetUserActiveStatusAsync(id, true);
        }

        public Task<GeneralResult> DeactivateUserAsync(string id)
        {
            return SetUserActiveStatusAsync(id, false);
        }
    }
}
