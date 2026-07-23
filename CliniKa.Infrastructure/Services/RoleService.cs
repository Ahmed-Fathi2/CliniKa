using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Role;
using CliniKa.Infrastructure.Persistence.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CliniKa.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IValidator<CreateRoleRequest> _createRoleValidator;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            IValidator<CreateRoleRequest> createRoleValidator,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _createRoleValidator = createRoleValidator;
            _logger = logger;
        }

        public async Task<GeneralResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
        {
            var validationResult = await _createRoleValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<RoleDto>.ValidationFailure(errors);
            }

            var roleExists = await _roleManager.RoleExistsAsync(request.RoleName);
            if (roleExists)
            {
                return GeneralResult<RoleDto>.Failure($"Role '{request.RoleName}' already exists.");
            }

            var role = new ApplicationRole
            {
                Name = request.RoleName,
                RoleDescription = request.Description
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var message = string.Join(", ", result.Errors.Select(e => e.Description));
                return GeneralResult<RoleDto>.Failure($"Failed to create role: {message}");
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.RoleDescription
            };

            return GeneralResult<RoleDto>.Success(roleDto, "Role created successfully.");
        }

        public async Task<GeneralResult<IEnumerable<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.RoleDescription
            });

            return GeneralResult<IEnumerable<RoleDto>>.Success(roleDtos, "Roles retrieved successfully.");
        }

        public async Task<GeneralResult<RoleDto>> GetRoleByIdAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return GeneralResult<RoleDto>.NotFound("Role not found.");
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.RoleDescription
            };

            return GeneralResult<RoleDto>.Success(roleDto, "Role retrieved successfully.");
        }
    }
}
