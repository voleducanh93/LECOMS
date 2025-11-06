using LECOMS.Common.Helper;
using LECOMS.Data.Entities;
using LECOMS.Service.Services;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/recombee")]
    public class RecombeeBrowseController : ControllerBase
    {
        private readonly RecombeeService _recombeeService;
        private readonly RecombeeBootstrap _bootstrap;
        private readonly UserManager<User> _userManager;
        private readonly ProductService _productService;
        public RecombeeBrowseController(
            RecombeeService recombeeService,
            RecombeeBootstrap bootstrap,
            UserManager<User> userManager, ProductService productService)
        {
            _recombeeService = recombeeService;
            _bootstrap = bootstrap;
            _userManager = userManager;
            _productService = productService;
        }

        // ===========================================================
        // 1️⃣ Khởi tạo schema (chỉ chạy 1 lần bởi Admin)
        // ===========================================================
        [HttpPost("init-schema")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> InitSchema()
        {
            await _bootstrap.InitSchemaAsync();
            return Ok(new { message = "✅ Schema Recommbee đã được khởi tạo thành công." });
        }

        // ===========================================================
        // 2️⃣ Đồng bộ toàn bộ sản phẩm sang Recommbee
        // ===========================================================
        [HttpPost("sync-products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncProducts()
        {
            var count = await _recombeeService.SyncProductsAsync();
            return Ok(new { message = $"✅ Đã đồng bộ {count} sản phẩm sang Recommbee." });
        }

        // ===========================================================
        // 3️⃣ API chính cho trang Shopping Browse
        // ===========================================================
        [HttpGet("browse")]
        [Authorize]
        public async Task<IActionResult> GetBrowse()
        {
            var userId = _userManager.GetUserId(User);
            var data = await _recombeeService.GetBrowseDataAsync(userId);
            return Ok(data);
        }

        // ===========================================================
        // 4️⃣ Gợi ý sản phẩm tương tự
        // ===========================================================
        [HttpGet("similar/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSimilarProducts(string productId)
        {
            var userId = _userManager.GetUserId(User) ?? "guest";
            var result = await _recombeeService.GetSimilarItemsAsync(productId, userId);
            return Ok(result);
        }

        [HttpGet("by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySlug(string slug, [FromServices] RecombeeTrackingService trackingService)
        {
            var response = new APIResponse();
            try
            {
                var product = await _productService.GetBySlugAsync(slug);

                // 🧠 Ghi hành vi xem sản phẩm
                var userId = _userManager.GetUserId(User) ?? "guest";
                await trackingService.TrackViewAsync(userId, product.Id);

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
