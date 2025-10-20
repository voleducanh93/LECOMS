using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Course;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseCategoryController : ControllerBase
    {
        private readonly ICourseCategoryService _service;

        public CourseCategoryController(ICourseCategoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách tất cả danh mục khoá học (public)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var response = new APIResponse();
            try
            {
                var list = await _service.GetAllAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Result = list;
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
        /// Admin tạo danh mục khóa học mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CourseCategoryCreateDTO dto)
        {
            var response = new APIResponse();
            try
            {
                var cat = await _service.CreateAsync(dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = cat;
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
    }
}
