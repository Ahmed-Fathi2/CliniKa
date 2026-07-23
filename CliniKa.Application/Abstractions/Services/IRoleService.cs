using System.Collections.Generic;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Role;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IRoleService
    {
        Task<GeneralResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
        Task<GeneralResult<IEnumerable<RoleDto>>> GetAllRolesAsync();
        Task<GeneralResult<RoleDto>> GetRoleByIdAsync(string id);
    }
}
