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
    [Authorize] // Seller phải đăng nhập
    public class SellerProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IShopService _shopService;
        private readonly UserManager<User> _userManager; // ✅ Thêm dòng này

        public SellerProductController(
            IProductService productService,
            IShopService shopService,
            UserManager<User> userManager) // ✅ Inject UserManager
        {
            _productService = productService;
            _shopService = shopService;
            _userManager = userManager;
        }

        /// <summary>
        /// Seller xem tất cả sản phẩm trong shop của mình
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            var response = new APIResponse();
            try
            {
                // ✅ Dùng UserManager để lấy đúng GUID userId
                var sellerId = _userManager.GetUserId(User);

                var shop = await _shopService.GetShopBySellerIdAsync(sellerId);
                if (shop == null)
                    throw new InvalidOperationException("Shop not found.");

                var list = await _productService.GetAllByShopAsync(shop.Id);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
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
        /// Seller tạo sản phẩm mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = _userManager.GetUserId(User); // ✅ sửa ở đây
                var shop = await _shopService.GetShopBySellerIdAsync(sellerId);
                if (shop == null)
                    throw new InvalidOperationException("Shop not found.");

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
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller cập nhật sản phẩm
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
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Seller xóa sản phẩm
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = new APIResponse();
            try
            {
                var deleted = await _productService.DeleteAsync(id);
                response.StatusCode = deleted ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                response.Result = new { deleted };
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
