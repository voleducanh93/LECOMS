using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>
        /// Seller đăng ký mở shop
        /// </summary>
        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> RegisterShop([FromBody] SellerRegistrationRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);

            try
            {
                var result = await _shopService.CreateShopAsync(userId, dto);
                return CreatedAtAction(nameof(GetMyShop), new { }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Seller xem shop của chính mình
        /// </summary>
        [HttpGet("my-shop")]
        [Authorize]
        public async Task<IActionResult> GetMyShop()
        {
            var userId = _userManager.GetUserId(User);
            var shop = await _shopService.GetShopBySellerIdAsync(userId);
            if (shop == null) return NotFound(new { message = "Shop not found." });
            return Ok(shop);
        }

        /// <summary>
        /// Seller cập nhật shop của mình
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateShop(int id, [FromBody] ShopUpdateDTO dto)
        {
            if (dto == null) return BadRequest("Invalid data.");
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            try
            {
                var result = await _shopService.UpdateShopAsync(id, dto, userId, isAdmin);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Shop not found." });
            }
        }

        /// <summary>
        /// Seller xóa shop của mình
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteShop(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            try
            {
                var deleted = await _shopService.DeleteShopAsync(id, userId, isAdmin);
                if (!deleted) return NotFound(new { message = "Shop not found." });
                return Ok(new { message = "Shop deleted successfully." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // ----------------- ADMIN FUNCTIONALITY -----------------

        /// <summary>
        /// Admin lấy tất cả shop (lọc theo trạng thái nếu cần)
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllShops([FromQuery] string? status = null)
        {
            var result = await _shopService.GetAllAsync(status);
            return Ok(result);
        }

        /// <summary>
        /// Admin xem chi tiết shop theo ID
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetShopById(int id)
        {
            var shop = await _shopService.GetByIdAsync(id);
            if (shop == null) return NotFound(new { message = "Shop not found." });
            return Ok(shop);
        }

        /// <summary>
        /// Admin duyệt shop
        /// </summary>
        [HttpPost("admin/{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveShop(int id)
        {
            var adminId = _userManager.GetUserId(User);
            try
            {
                var result = await _shopService.ApproveShopAsync(id, adminId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Shop not found." });
            }
        }

        /// <summary>
        /// Admin từ chối shop (có lý do)
        /// </summary>
        [HttpPost("admin/{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectShop(int id, [FromBody] RejectShopRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = _userManager.GetUserId(User);
            try
            {
                var result = await _shopService.RejectShopAsync(id, adminId, dto.Reason);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Shop not found." });
            }
        }
    }
}
