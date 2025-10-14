using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/seller/courses")]
public class SellerCourseController : ControllerBase
{
    private readonly ISellerCourseService _service;

    public SellerCourseController(ISellerCourseService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseDto dto)
    {
        var course = await _service.CreateCourseAsync(dto);
        return Ok(course);
    }

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(CreateSectionDto dto)
    {
        var section = await _service.CreateSectionAsync(dto);
        return Ok(section);
    }

    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson(CreateLessonDto dto)
    {
        var lesson = await _service.CreateLessonAsync(dto);
        return Ok(lesson);
    }

    [HttpPost("products")]
    public async Task<IActionResult> LinkCourseProduct(LinkCourseProductDto dto)
    {
        var cp = await _service.LinkCourseProductAsync(dto);
        return Ok(cp);
    }
}
