using LECOMS.Common.Helper;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ISellerCourseService _courseService;
        private readonly IUnitOfWork _uow;

        public HomeController(
            IProductService productService,
            ISellerCourseService courseService,
            IUnitOfWork uow)
        {
            _productService = productService;
            _courseService = courseService;
            _uow = uow;
        }

        /// <summary>
        /// 🔹 Lấy danh sách sản phẩm public (homepage)
        /// </summary>
        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sort = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null
        )
        {
            var response = new APIResponse();

            try
            {
                var data = await _productService.GetPublicProductsAsync(
                    search, category, sort, page, pageSize, minPrice, maxPrice
                );
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
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
        /// 🔹 Lấy danh sách khóa học public (homepage)
        /// </summary>
        [HttpGet("courses")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourses([FromQuery] int limit = 10, [FromQuery] string? category = null)
        {
            var response = new APIResponse();

            try
            {
                var data = await _courseService.GetPublicCoursesAsync(limit, category);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
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
        /// 🔹 Lấy thông tin sản phẩm public theo slug (trang chi tiết sản phẩm)
        /// </summary>
        [HttpGet("products/by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductBySlug(string slug)
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
