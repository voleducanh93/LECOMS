using LECOMS.Common.Helper;
using LECOMS.Data.Entities;
using LECOMS.Service.Services;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/recombee")]
    public class RecombeeBrowseController : ControllerBase
    {
        private readonly RecombeeService _recombee;
        private readonly RecombeeTrackingService _tracking;
        private readonly UserManager<User> _userManager;
        private readonly IProductService _productService;
        private readonly ISellerCourseService _courseService;

        public RecombeeBrowseController(
            RecombeeService recombee,
            RecombeeTrackingService tracking,
            UserManager<User> userManager,
            IProductService productService,
            ISellerCourseService courseService)
        {
            _recombee = recombee;
            _tracking = tracking;
            _userManager = userManager;
            _productService = productService;
            _courseService = courseService;
        }

        // =============================================================
        // 1) RECOMMEND PRODUCT — HOMEPAGE
        // =============================================================
        [HttpGet("browse/products")]
        [Authorize]
        public async Task<IActionResult> BrowseProducts()
        {
            var res = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);

                res.StatusCode = HttpStatusCode.OK;
                res.Result = await _recombee.RecommendProductsForUserAsync(userId);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.ErrorMessages.Add(ex.Message);
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return StatusCode((int)res.StatusCode, res);
        }

        // =============================================================
        // 2) RECOMMEND COURSE — HOMEPAGE
        // =============================================================
        [HttpGet("browse/courses")]
        [Authorize]
        public async Task<IActionResult> BrowseCourses()
        {
            var res = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);

                res.StatusCode = HttpStatusCode.OK;
                res.Result = await _recombee.RecommendCoursesForUserAsync(userId);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.ErrorMessages.Add(ex.Message);
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return StatusCode((int)res.StatusCode, res);
        }

        // =============================================================
        // 3) RECOMMEND PRODUCT — ITEM-TO-ITEM SIMILARITY
        // =============================================================
        [HttpGet("product/{slug}/recommend")]
        [AllowAnonymous]
        public async Task<IActionResult> RecommendProductsForProduct(string slug)
        {
            var res = new APIResponse();

            try
            {
                var product = await _productService.GetBySlugAsync(slug);
                var userId = _userManager.GetUserId(User) ?? "guest";

                // Ghi tracking xem sản phẩm
                await _tracking.TrackViewAsync(userId, product.Id);

                res.StatusCode = HttpStatusCode.OK;
                res.Result = await _recombee.GetSimilarProductsFullAsync(product.Id, userId);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.ErrorMessages.Add(ex.Message);
                res.StatusCode = HttpStatusCode.BadRequest;
            }

            return StatusCode((int)res.StatusCode, res);
        }

        // =============================================================
        // 4) RECOMMEND COURSE — ITEM-TO-ITEM SIMILARITY
        // =============================================================
        [HttpGet("course/{slug}/recommend")]
        [AllowAnonymous]
        public async Task<IActionResult> RecommendCoursesForCourse(string slug)
        {
            var res = new APIResponse();

            try
            {
                var course = await _courseService.GetCourseBySlugAsync(slug);
                var userId = _userManager.GetUserId(User) ?? "guest";

                // track view khóa học
                await _tracking.TrackViewAsync(userId, course.Id);

                res.StatusCode = HttpStatusCode.OK;
                res.Result = await _recombee.GetSimilarCoursesFullAsync(course.Id, userId);
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.ErrorMessages.Add(ex.Message);
                res.StatusCode = HttpStatusCode.BadRequest;
            }

            return StatusCode((int)res.StatusCode, res);
        }
    }
}
