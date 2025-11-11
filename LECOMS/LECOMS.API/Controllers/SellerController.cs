using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellerController : ControllerBase
    {
        private readonly IShopService _shopService;
        private readonly UserManager<User> _userManager;

        public SellerController(IShopService shopService, UserManager<User> userManager)
        {
            _shopService = shopService;
            _userManager = userManager;
        }

        // -----------------------------------------------------------
        // SELLER FUNCTIONALITY
        // -----------------------------------------------------------

        /// <summary>
        /// Seller đăng ký mở shop
        /// </summary>
        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> RegisterShop([FromBody] SellerRegistrationRequestDTO dto)
        {
            var response = new APIResponse();

            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Invalid data.");
                return StatusCode((int)response.StatusCode, response);
            }

            var userId = _userManager.GetUserId(User);

            try
            {
                var result = await _shopService.CreateShopAsync(userId, dto);

                // ✅ Gán role Seller cho user
                var user = await _userManager.FindByIdAsync(userId);
                // ⚙️ Nếu user đang là Customer → remove luôn
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Customer");
                }

                // ✅ Thêm role Seller (nếu chưa có)
                if (!await _userManager.IsInRoleAsync(user, "Seller"))
                {
                    await _userManager.AddToRoleAsync(user, "Seller");
                }

                // 🔄 Reset refresh token để sinh token mới
                user.RefreshToken = null;
                await _userManager.UpdateAsync(user);


                response.StatusCode = HttpStatusCode.Created;
                response.Result = result;
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Internal error: {ex.Message}");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller xem shop của chính mình
        /// </summary>
        [HttpGet("my-shop")]
        [Authorize]
        public async Task<IActionResult> GetMyShop()
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);

            try
            {
                var shop = await _shopService.GetShopBySellerIdAsync(userId);
                if (shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Shop not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = shop;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller cập nhật shop của mình
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateShop(int id, [FromBody] ShopUpdateDTO dto)
        {
            var response = new APIResponse();

            if (dto == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Invalid data.");
                return StatusCode((int)response.StatusCode, response);
            }

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            try
            {
                var result = await _shopService.UpdateShopAsync(id, dto, userId, isAdmin);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (UnauthorizedAccessException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add("You are not authorized to update this shop.");
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Shop not found.");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller xóa shop của mình
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteShop(int id)
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            try
            {
                var deleted = await _shopService.DeleteShopAsync(id, userId, isAdmin);
                if (!deleted)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Shop not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = new { message = "Shop deleted successfully." };
                }
            }
            catch (UnauthorizedAccessException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add("You are not authorized to delete this shop.");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // -----------------------------------------------------------
        // ADMIN FUNCTIONALITY
        // -----------------------------------------------------------

        /// <summary>
        /// Admin lấy tất cả shop (lọc theo trạng thái nếu cần)
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllShops([FromQuery] string? status = null)
        {
            var response = new APIResponse();
            try
            {
                var result = await _shopService.GetAllAsync(status);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Admin xem chi tiết shop theo ID
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetShopById(int id)
        {
            var response = new APIResponse();
            try
            {
                var shop = await _shopService.GetByIdAsync(id);
                if (shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Shop not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = shop;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Admin duyệt shop
        /// </summary>
        [HttpPost("admin/{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveShop(int id)
        {
            var response = new APIResponse();
            try
            {
                var adminId = _userManager.GetUserId(User);
                var result = await _shopService.ApproveShopAsync(id, adminId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Shop not found.");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Admin từ chối shop (có lý do)
        /// </summary>
        [HttpPost("admin/{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectShop(int id, [FromBody] RejectShopRequest dto)
        {
            var response = new APIResponse();

            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Invalid request.");
                return StatusCode((int)response.StatusCode, response);
            }

            var adminId = _userManager.GetUserId(User);
            try
            {
                var result = await _shopService.RejectShopAsync(id, adminId, dto.Reason);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Shop not found.");
            }

            return StatusCode((int)response.StatusCode, response);
        }
        
    }
}
