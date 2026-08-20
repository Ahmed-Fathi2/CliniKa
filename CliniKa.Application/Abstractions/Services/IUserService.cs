using System.Collections.Generic;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.User;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IUserService
    {
        Task<GeneralResult<UserDto>> CreateUserAsync(CreateUserRequest request);
        Task<GeneralResult<IEnumerable<UserDto>>> GetAllUsersAsync();
        Task<GeneralResult<UserDto>> GetUserByIdAsync(string id);
        Task<GeneralResult<UserDto>> UpdateUserProfileAsync(string id, string firstName, string lastName, string? phoneNumber);
        Task<GeneralResult> ActivateUserAsync(string id);
        Task<GeneralResult> DeactivateUserAsync(string id);
        Task<GeneralResult> SetUserActiveStatusAsync(string id, bool isActive);
    }
}
