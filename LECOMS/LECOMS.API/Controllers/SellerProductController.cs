using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/seller/products")]
    [Authorize(Roles = "Seller, Admin")] // ✅ chỉ seller có quyền truy cập
    public class SellerProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IShopService _shopService;
        private readonly UserManager<User> _userManager;

        public SellerProductController(
            IProductService productService,
            IShopService shopService,
            UserManager<User> userManager)
        {
            _productService = productService;
            _shopService = shopService;
            _userManager = userManager;
        }

        /// <summary>
        /// Seller xem tất cả sản phẩm trong shop của mình (bao gồm hình ảnh)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            var response = new APIResponse();
            try
            {
                var sellerId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(sellerId))
                    throw new UnauthorizedAccessException("Invalid user.");

                var shop = await _shopService.GetShopBySellerIdAsync(sellerId);
                if (shop == null)
                    throw new InvalidOperationException("Shop not found.");

                var list = await _productService.GetAllByShopAsync(shop.Id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller xem chi tiết 1 sản phẩm (bao gồm hình ảnh)
        /// </summary>
        // Lấy theo ID
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = new APIResponse();
            try
            {
                var product = await _productService.GetByIdAsync(id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = product;
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller tạo sản phẩm mới (có thể thêm nhiều ảnh)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var sellerId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(sellerId))
                    throw new UnauthorizedAccessException("Invalid user.");

                var shop = await _shopService.GetShopBySellerIdAsync(sellerId);
                if (shop == null)
                    throw new InvalidOperationException("Shop not found.");

                // ✅ gọi service có xử lý images, status, lastUpdatedAt
                var product = await _productService.CreateAsync(shop.Id, dto);

                response.StatusCode = HttpStatusCode.Created;
                response.Result = product;
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Internal Error: {ex.Message}");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller cập nhật sản phẩm (bao gồm trạng thái, nhiều hình ảnh)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var updated = await _productService.UpdateAsync(id, dto);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = updated;
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
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
                response.ErrorMessages.Add($"Internal Error: {ex.Message}");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller xóa sản phẩm (tự động xóa ảnh liên quan)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = new APIResponse();
            try
            {
                var deleted = await _productService.DeleteAsync(id);
                if (!deleted)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Product not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = new { message = "Product deleted successfully." };
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Error deleting product: {ex.Message}");
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller cập nhật trạng thái sản phẩm (Draft / Published / OutOfStock / Archived)
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] ProductStatusUpdateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var updateDto = new ProductUpdateDTO
                {
                    Status = dto.Status
                };
                var updated = await _productService.UpdateAsync(id, updateDto);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = updated;
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Error updating status: {ex.Message}");
            }

            return StatusCode((int)response.StatusCode, response);
        }
        /// <summary>
        /// Lấy thông tin sản phẩm public theo slug
        /// </summary>
        // Lấy theo Slug (public view)
        [AllowAnonymous]
        [HttpGet("by-slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var response = new APIResponse();
            try
            {
                var product = await _productService.GetBySlugAsync(slug);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = product;
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Product not found.");
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
