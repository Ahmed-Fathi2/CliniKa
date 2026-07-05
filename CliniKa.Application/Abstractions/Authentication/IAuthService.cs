using CliniKa.Application.DTOs;
using System.Threading.Tasks;

namespace CliniKa.Application.Abstraction
{
    public interface IAuthService
    {
        Task<GeneralResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
        Task<GeneralResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);

        Task<GeneralResult<RegisterResponse>> RegisterAsync(RegisterRequest registerRequest);
        
        Task<GeneralResult> ForgetPasswordAsync(ForgetPasswordRequest forgetPasswordRequest);
        Task<GeneralResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);


        Task ForceResetPass();
    }
}
