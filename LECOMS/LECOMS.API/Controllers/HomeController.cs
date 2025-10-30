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
        public async Task<IActionResult> GetProducts([FromQuery] int limit = 10, [FromQuery] string? category = null)
        {
            var response = new APIResponse();

            try
            {
                var data = await _productService.GetPublicProductsAsync(limit, category);
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
        /// 🔹 Lấy danh sách danh mục sản phẩm (cho dropdown FE)
        /// </summary>
        [HttpGet("product-categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductCategories()
        {
            var response = new APIResponse();

            try
            {
                var data = await _uow.ProductCategories.GetAllAsync(c => c.Active == 1);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug
                });
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
        /// 🔹 Lấy danh sách danh mục khóa học (cho dropdown FE)
        /// </summary>
        [HttpGet("course-categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseCategories()
        {
            var response = new APIResponse();

            try
            {
                var data = await _uow.CourseCategories.GetAllAsync(c => c.Active == 1);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug
                });
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
