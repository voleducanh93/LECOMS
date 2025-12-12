using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/seller/courses")]
    [Authorize(Roles = "Seller, Admin")] // ✅ chỉ seller có thể tạo và quản lý course
    public class SellerCourseController : ControllerBase
    {
        private readonly ISellerCourseService _service;

        public SellerCourseController(ISellerCourseService service)
        {
            _service = service;
        }

        /// <summary>
        /// Seller tạo khóa học mới (có hình thumbnail)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCourse(
    [FromBody] CreateCourseDto dto,
    [FromServices] IShopService shopService,
    [FromServices] UserManager<User> userManager)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = userManager.GetUserId(User);
                var shop = await shopService.GetShopBySellerIdAsync(sellerId);
                if (shop == null)
                    throw new InvalidOperationException("Không tìm thấy cửa hàng.");

                // 🔥 gán shopId
                dto.ShopId = shop.Id;

                var course = await _service.CreateCourseAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = course;
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
        /// Seller tạo Section (phần trong khóa học)
        /// </summary>
        [HttpPost("sections")]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var section = await _service.CreateSectionAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = section;
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
        /// Seller tạo bài học (Lesson)
        /// </summary>
        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
        {
            var response = new APIResponse();
            try
            {
                var lesson = await _service.CreateLessonAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = lesson;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        // ================================================================
        // ✅ PHẦN MỚI: Liên kết sản phẩm với bài học (Lesson–Product)
        // ================================================================

        /// <summary>
        /// Seller liên kết sản phẩm với bài học (Lesson)
        /// </summary>
        [HttpPost("lessons/products")]
        public async Task<IActionResult> LinkLessonProduct([FromBody] LinkLessonProductDto dto)
        {
            var response = new APIResponse();
            try
            {
                var link = await _service.LinkLessonProductAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = link;
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
        /// Seller hủy liên kết sản phẩm với bài học
        /// </summary>
        [HttpDelete("lessons/{lessonId}/products/{productId}")]
        public async Task<IActionResult> UnlinkLessonProduct(string lessonId, string productId)
        {
            var response = new APIResponse();
            try
            {
                var ok = await _service.UnlinkLessonProductAsync(lessonId, productId);
                response.StatusCode = ok ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                response.Result = new { deleted = ok };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        // ================================================================
        // ✅ PHẦN MỚI: Xóa bài học hoặc section
        // ================================================================

        /// <summary>
        /// Seller xóa bài học (Lesson) và toàn bộ liên kết sản phẩm
        /// </summary>
        [HttpDelete("lessons/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(string lessonId)
        {
            var response = new APIResponse();
            try
            {
                var ok = await _service.DeleteLessonAsync(lessonId);
                response.StatusCode = ok ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                response.Result = new { deleted = ok };
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
        /// Seller xóa Section (và toàn bộ bài học bên trong + liên kết sản phẩm)
        /// </summary>
        [HttpDelete("sections/{sectionId}")]
        public async Task<IActionResult> DeleteSection(string sectionId)
        {
            var response = new APIResponse();
            try
            {
                var ok = await _service.DeleteSectionAsync(sectionId);
                response.StatusCode = ok ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                response.Result = new { deleted = ok };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCourses([FromServices] UserManager<User> userManager)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = userManager.GetUserId(User);
                var courses = await _service.GetCoursesBySellerAsync(sellerId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = courses;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(string id, [FromServices] UserManager<User> userManager)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = userManager.GetUserId(User);
                var course = await _service.GetCourseByIdAsync(id, sellerId);
                if (course == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Course không tìm thấy");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = course;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(string id, [FromBody] UpdateCourseDto dto, [FromServices] UserManager<User> userManager)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = userManager.GetUserId(User);
                var result = await _service.UpdateCourseAsync(id, dto, sellerId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(string id, [FromServices] UserManager<User> userManager)
        {
            var response = new APIResponse();
            try
            {
                var sellerId = userManager.GetUserId(User);
                var deleted = await _service.DeleteCourseAsync(id, sellerId);
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

        [HttpGet("{courseId}/sections")]
        public async Task<IActionResult> GetSectionsByCourse(string courseId)
        {
            var response = new APIResponse();
            try
            {
                // Lấy danh sách section theo course
                var sections = await _service.GetSectionsByCourseAsync(courseId, true);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = sections;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("sections/{sectionId}/lessons")]
        public async Task<IActionResult> GetLessonsBySection(string sectionId)
        {
            var response = new APIResponse();
            try
            {
                var lessons = await _service.GetLessonsBySectionAsync(sectionId, true);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = lessons;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("sections/{id}")]
        public async Task<IActionResult> UpdateSection(string id, [FromBody] CreateSectionDto dto)
        {
            var response = new APIResponse();
            try
            {
                if (dto == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Payload cannot be null.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var result = await _service.UpdateSectionAsync(id, dto);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
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


        [HttpPut("lessons/{id}")]
        public async Task<IActionResult> UpdateLesson(string id, [FromBody] CreateLessonDto dto)
        {
            var response = new APIResponse();
            try
            {
                if (dto == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.ErrorMessages.Add("Payload cannot be null.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var result = await _service.UpdateLessonAsync(id, dto);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
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


    }
}
