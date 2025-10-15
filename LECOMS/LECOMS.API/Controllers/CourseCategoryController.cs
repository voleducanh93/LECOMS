using LECOMS.Data.DTOs.Course;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CourseCategoryCreateDTO dto)
        {
            try
            {
                var cat = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetAll), new { id = cat.Id }, cat);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
