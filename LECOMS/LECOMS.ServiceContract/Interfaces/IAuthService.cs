using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(UserRegisterDTO dto);
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO loginRequestDTO);
        Task LogoutAsync(string refreshToken);
        Task<LoginResponseDTO> RefreshTokenAsync(string refreshToken);
        Task<bool> ForgetPasswordAsync(string email);
        Task<(bool Success, string Message)> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
