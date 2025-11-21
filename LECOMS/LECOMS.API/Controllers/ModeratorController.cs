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

        // ========================== COURSE =============================

        [HttpGet("courses/pending")]
        public async Task<IActionResult> GetPendingCourses()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Courses.Query()
                    .Where(c => c.ApprovalStatus == ApprovalStatus.Pending)
                    .Include(c => c.Shop)
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
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("courses/{id}/approve")]
        public async Task<IActionResult> ApproveCourse(string id)
        {
            var course = await _uow.Courses.GetAsync(c => c.Id == id);
            if (course == null) return NotFound();

            course.ApprovalStatus = ApprovalStatus.Approved;
            course.ModeratorNote = null;

            await _uow.Courses.UpdateAsync(course);
            await _uow.CompleteAsync();

            return Ok(new APIResponse { StatusCode = HttpStatusCode.OK, Result = "Approved" });
        }

        [HttpPost("courses/{id}/reject")]
        public async Task<IActionResult> RejectCourse(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var course = await _uow.Courses.GetAsync(c => c.Id == id);
            if (course == null) return NotFound();

            course.ApprovalStatus = ApprovalStatus.Rejected;
            course.ModeratorNote = dto.Reason;

            await _uow.Courses.UpdateAsync(course);
            await _uow.CompleteAsync();

            return Ok(new APIResponse { StatusCode = HttpStatusCode.OK, Result = "Rejected" });
        }


        // ========================== PRODUCT =============================

        [HttpGet("products/pending")]
        public async Task<IActionResult> GetPendingProducts()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Products.Query()
                    .Where(p => p.ApprovalStatus == ApprovalStatus.Pending)
                    .Include(p => p.Shop)
                    .Include(p => p.Images)
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
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("products/{id}/approve")]
        public async Task<IActionResult> ApproveProduct(string id)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id);
            if (product == null) return NotFound();

            product.ApprovalStatus = ApprovalStatus.Approved;
            product.ModeratorNote = null;

            // ⭐ Publish sản phẩm ngay sau khi duyệt
            product.Status = ProductStatus.Published;

            await _uow.Products.UpdateAsync(product);
            await _uow.CompleteAsync();

            return Ok(new APIResponse { StatusCode = HttpStatusCode.OK, Result = "Approved" });
        }

        [HttpPost("products/{id}/reject")]
        public async Task<IActionResult> RejectProduct(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var product = await _uow.Products.GetAsync(p => p.Id == id);
            if (product == null) return NotFound();

            product.ApprovalStatus = ApprovalStatus.Rejected;
            product.ModeratorNote = dto.Reason;

            // ⭐ Chuyển về draft
            product.Status = ProductStatus.Draft;

            await _uow.Products.UpdateAsync(product);
            await _uow.CompleteAsync();

            return Ok(new APIResponse { StatusCode = HttpStatusCode.OK, Result = "Rejected" });
        }
    }
}
