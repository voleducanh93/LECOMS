using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Moderation;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/moderator")]
    [Authorize(Roles = "Moderator, Admin")]
    public class ModeratorController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public ModeratorController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================================================================
        //                       COURSE MODERATION
        // ================================================================

        [HttpGet("courses/pending")]
        public async Task<IActionResult> GetPendingCourses()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Courses.Query()
                    .Include(c => c.Shop)
                    .Where(c => c.ApprovalStatus == ApprovalStatus.Pending)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Slug,
                        ShopName = c.Shop.Name
                    })
                    .ToListAsync();

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

        [HttpPost("courses/{id}/approve")]
        public async Task<IActionResult> ApproveCourse(string id)
        {
            var response = new APIResponse();
            try
            {
                var course = await _uow.Courses.GetAsync(c => c.Id == id);
                if (course == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Course not found.");
                    return StatusCode((int)response.StatusCode, response);
                }

                course.ApprovalStatus = ApprovalStatus.Approved;
                course.ModeratorNote = null;

                await _uow.Courses.UpdateAsync(course);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Approved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("courses/{id}/reject")]
        public async Task<IActionResult> RejectCourse(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var course = await _uow.Courses.GetAsync(c => c.Id == id);
                if (course == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Course not found.");
                    return StatusCode((int)response.StatusCode, response);
                }

                course.ApprovalStatus = ApprovalStatus.Rejected;
                course.ModeratorNote = dto.Reason;

                await _uow.Courses.UpdateAsync(course);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Rejected";
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
        //                       PRODUCT MODERATION
        // ================================================================

        [HttpGet("products/pending")]
        public async Task<IActionResult> GetPendingProducts()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Products.Query()
                    .Include(p => p.Shop)
                    .Include(p => p.Images)
                    .Where(p => p.ApprovalStatus == ApprovalStatus.Pending)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        ShopName = p.Shop.Name,
                        Thumbnail = p.Images
                            .OrderBy(i => i.OrderIndex)
                            .Select(i => i.Url)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

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

        [HttpPost("products/{id}/approve")]
        public async Task<IActionResult> ApproveProduct(string id)
        {
            var response = new APIResponse();
            try
            {
                var product = await _uow.Products.GetAsync(p => p.Id == id);
                if (product == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Product not found.");
                    return StatusCode((int)response.StatusCode, response);
                }

                product.ApprovalStatus = ApprovalStatus.Approved;
                product.ModeratorNote = null;
                product.Status = ProductStatus.Published;

                await _uow.Products.UpdateAsync(product);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Approved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("products/{id}/reject")]
        public async Task<IActionResult> RejectProduct(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var product = await _uow.Products.GetAsync(p => p.Id == id);
                if (product == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Product not found.");
                    return StatusCode((int)response.StatusCode, response);
                }

                product.ApprovalStatus = ApprovalStatus.Rejected;
                product.ModeratorNote = dto.Reason;
                product.Status = ProductStatus.Draft;

                await _uow.Products.UpdateAsync(product);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Rejected";
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
        //                       SECTION MODERATION
        // ================================================================

        [HttpGet("sections/pending")]
        public async Task<IActionResult> GetPendingSections()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Sections.Query()
                    .Include(s => s.Course)
                    .Where(s => s.ApprovalStatus == ApprovalStatus.Pending)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        CourseTitle = s.Course.Title
                    })
                    .ToListAsync();

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

        [HttpPost("sections/{id}/approve")]
        public async Task<IActionResult> ApproveSection(string id)
        {
            var response = new APIResponse();
            try
            {
                var section = await _uow.Sections.GetAsync(s => s.Id == id);
                if (section == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Không tìm thấy phần.");
                    return StatusCode((int)response.StatusCode, response);
                }

                section.ApprovalStatus = ApprovalStatus.Approved;
                section.ModeratorNote = null;

                await _uow.Sections.UpdateAsync(section);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Section Approved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("sections/{id}/reject")]
        public async Task<IActionResult> RejectSection(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var section = await _uow.Sections.GetAsync(s => s.Id == id);
                if (section == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Không tìm thấy phần.");
                    return StatusCode((int)response.StatusCode, response);
                }

                section.ApprovalStatus = ApprovalStatus.Rejected;
                section.ModeratorNote = dto.Reason;

                await _uow.Sections.UpdateAsync(section);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Phần bị từ chối";
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
        //                       LESSON MODERATION
        // ================================================================

        [HttpGet("lessons/pending")]
        public async Task<IActionResult> GetPendingLessons()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Lessons.Query()
                    .Include(l => l.Section)
                    .ThenInclude(s => s.Course)
                    .Where(l => l.ApprovalStatus == ApprovalStatus.Pending)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        SectionTitle = l.Section.Title,
                        CourseTitle = l.Section.Course.Title
                    })
                    .ToListAsync();

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

        [HttpPost("lessons/{id}/approve")]
        public async Task<IActionResult> ApproveLesson(string id)
        {
            var response = new APIResponse();
            try
            {
                var lesson = await _uow.Lessons.GetAsync(l => l.Id == id);
                if (lesson == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Bài học không tìm thấy.");
                    return StatusCode((int)response.StatusCode, response);
                }

                lesson.ApprovalStatus = ApprovalStatus.Approved;
                lesson.ModeratorNote = null;

                await _uow.Lessons.UpdateAsync(lesson);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Bài học đã được phê duyệt";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        [HttpPost("lessons/{id}/reject")]
        public async Task<IActionResult> RejectLesson(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var lesson = await _uow.Lessons.GetAsync(l => l.Id == id);
                if (lesson == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Bài học không tìm thấy.");
                    return StatusCode((int)response.StatusCode, response);
                }

                lesson.ApprovalStatus = ApprovalStatus.Rejected;
                lesson.ModeratorNote = dto.Reason;

                await _uow.Lessons.UpdateAsync(lesson);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Bài học bị từ chối";
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
