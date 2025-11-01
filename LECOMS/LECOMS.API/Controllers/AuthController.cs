using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.Email;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly APIResponse _response;
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, APIResponse response, UserManager<User> userManager)
        {
            _authService = authService;
            _response = response;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            try
            {
                var user = await _authService.RegisterAsync(dto);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new { Message = "Đăng ký thành công. Vui lòng xác nhận email của bạn." };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        [HttpPost("confirm-email")]
        //[Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest model)
        {
            try
            {
                var result = await _authService.ConfirmEmailAsync(model.Email, model.Token);

                // Check for success and handle different cases
                if (!result)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Token không hợp lệ hoặc đã hết hạn.");
                    return BadRequest(_response);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
           //     await _walletService.CreateWalletAsync(user.Id, isAdminWallet: false);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new { Message = "Email đã được xác nhận thành công." };
                return Ok(_response);
            }

            catch (Exception ex)
            {
                // Handling exception for already confirmed email
                if (ex.Message == "Email đã được xác nhận.")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Result = new { Message = "Email đã được xác nhận." };

                    return Ok(_response);
                }

                // Handle other exceptions
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequestDTO)
        {
            try
            {
                var result = await _authService.LoginAsync(loginRequestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = result;
                return Ok(_response);
            }
            catch (KeyNotFoundException) // Nếu tài khoản không tồn tại
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Tài khoản không tồn tại.");
                return NotFound(_response);
            }
            catch (Exception ex) // Nếu sai mật khẩu hoặc email chưa xác nhận hoặc lỗi hệ thống khác
            {
                if (ex.Message.Contains("Mật khẩu không chính xác") || ex.Message.Contains("Email chưa được xác nhận"))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add(ex.Message);
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Lỗi hệ thống: {ex.Message}");
                return BadRequest(_response);
            }
        }


        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO model)
        {
            // Kiểm tra nếu RefreshToken không có trong yêu cầu
            if (string.IsNullOrWhiteSpace(model.RefreshToken))
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Cần phải có Refresh token làm mới.");
                return BadRequest(_response);
            }

            try
            {
                // Gọi dịch vụ để làm mới token
                var result = await _authService.RefreshTokenAsync(model.RefreshToken);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        [AllowAnonymous]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDTO model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Cần phải có Email.");
                return BadRequest(_response);
            }

            try
            {
                await _authService.ForgetPasswordAsync(model.Email);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new { Message = "Đã gửi liên kết đặt lại mật khẩu." };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return BadRequest(_response);
            }
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Cần phải nhập Email, Token và Mật khẩu mới.");
                    return BadRequest(_response);
                }

                var (success, message) = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);

                if (!success)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add(message);
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = new { Success = true, Message = message };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add($"Lỗi hệ thống: {ex.Message}");
                return BadRequest(_response);
            }
        }
        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmailGet([FromQuery] string email, [FromQuery] string token)
        {
            try
            {
                var result = await _authService.ConfirmEmailAsync(email, token);

                if (result)
                {
                    // ✅ Confirm thành công → redirect về FE
                    return Redirect("https://lecom-fe.vercel.app/auth/email-confirmed");
                }
                else
                {
                    // ❌ Token sai → redirect về trang lỗi
                    return Redirect("https://lecom-fe.vercel.app/auth/email-failed");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Email đã được xác nhận.")
                {
                    // ⚠️ Đã confirm rồi → vẫn redirect về trang thành công
                    return Redirect("https://lecom-fe.vercel.app/auth/email-confirmed");
                }

                // Lỗi khác → redirect về trang lỗi
                return Redirect("https://lecom-fe.vercel.app/auth/email-failed");
            }
        }
        [AllowAnonymous]
        [HttpGet("reset-password")]
        public IActionResult RedirectToResetPassword([FromQuery] string email, [FromQuery] string token)
        {
            try
            {
                // ✅ Encode token lần nữa khi chuyển qua FE
                var encodedToken = Uri.EscapeDataString(token);
                var frontendUrl = $"https://lecom-fe.vercel.app/auth/reset-password?email={email}&token={encodedToken}";

                // Redirect FE, tại FE sẽ có form nhập mật khẩu mới
                return Redirect(frontendUrl);
            }
            catch (Exception ex)
            {
                return Content($"<h3 style='color:red'>❌ Lỗi redirect reset password: {ex.Message}</h3>", "text/html");
            }
        }


    }
}
