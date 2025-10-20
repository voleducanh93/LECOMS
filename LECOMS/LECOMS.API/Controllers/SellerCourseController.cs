using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Course;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/seller/courses")]
    [Authorize] // ✅ chỉ seller có thể tạo course
    public class SellerCourseController : ControllerBase
    {
        private readonly ISellerCourseService _service;

        public SellerCourseController(ISellerCourseService service)
        {
            _service = service;
        }

        /// <summary>
        /// Seller tạo khóa học mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            var response = new APIResponse();
            try
            {
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

        /// <summary>
        /// Seller liên kết khóa học với sản phẩm
        /// </summary>
        [HttpPost("products")]
        public async Task<IActionResult> LinkCourseProduct([FromBody] LinkCourseProductDto dto)
        {
            var response = new APIResponse();
            try
            {
                var cp = await _service.LinkCourseProductAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = cp;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
