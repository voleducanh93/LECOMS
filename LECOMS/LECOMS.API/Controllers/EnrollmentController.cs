using LECOMS.Common.Helper;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly UserManager<User> _userManager;

        public EnrollmentController(IEnrollmentService enrollmentService, UserManager<User> userManager)
        {
            _enrollmentService = enrollmentService;
            _userManager = userManager;
        }

        [HttpPost("{courseId}/enroll")]
        public async Task<IActionResult> Enroll(string courseId)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException("User not found.");

                var enrollment = await _enrollmentService.EnrollAsync(userId, courseId);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = enrollment;
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Unauthorized;
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

        [HttpGet("{courseId}/enrollment")]
        public async Task<IActionResult> GetEnrollment(string courseId)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                var enrollment = await _enrollmentService.GetEnrollmentAsync(userId, courseId);
                if (enrollment == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Enrollment not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = enrollment;
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

        [HttpGet("my-enrollments")]
        public async Task<IActionResult> GetMyEnrollments()
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("User not found.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var result = await _enrollmentService.GetUserEnrollmentsAsync(userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
        [HttpGet("{courseId}/learn")]
        public async Task<IActionResult> GetLearningDetail(string courseId)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                var data = await _enrollmentService.GetLearningDetailAsync(userId, courseId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
        [HttpPost("lessons/{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(string lessonId)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                var ok = await _enrollmentService.CompleteLessonAsync(userId, lessonId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = ok;
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