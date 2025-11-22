using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Community;
using LECOMS.Data.DTOs.Moderation;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/community")]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityService _service;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _uow;

        public CommunityController(
            ICommunityService service,
            UserManager<User> userManager,
            IUnitOfWork uow)
        {
            _service = service;
            _userManager = userManager;
            _uow = uow;
        }

        // ======================================================
        // 👤 USER ENDPOINTS (Customer/Seller/Admin) - Community
        // ======================================================

        /// <summary>
        /// Tạo bài viết mới trong cộng đồng (mặc định Pending chờ duyệt)
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Invalid user.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var post = await _service.CreatePostAsync(userId, dto.Title, dto.Body);

                response.StatusCode = HttpStatusCode.Created;
                response.Result = post;
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
        /// Lấy danh sách bài viết cộng đồng (chỉ bài đã được Moderator duyệt)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var response = new APIResponse();
            try
            {
                var posts = await _service.GetPublicPostsAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = posts;
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
        /// Thêm bình luận vào một bài viết cộng đồng
        /// </summary>
        [Authorize]
        [HttpPost("{postId}/comment")]
        public async Task<IActionResult> Comment(string postId, [FromBody] CreateCommentDto dto)
        {
            var response = new APIResponse();
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Invalid user.");
                    return StatusCode((int)response.StatusCode, response);
                }

                var comment = await _service.CreateCommentAsync(userId, postId, dto.Body);

                response.StatusCode = HttpStatusCode.Created;
                response.Result = comment;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        // ======================================================
        // 🛡 MODERATOR ENDPOINTS - Kiểm duyệt CommunityPost
        // ======================================================

        /// <summary>
        /// Moderator xem danh sách bài viết cộng đồng đang chờ duyệt
        /// </summary>
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingCommunityPosts()
        {
            var data = await _uow.CommunityPosts.GetPendingAsync();

            var result = data.Select(p => new CommunityPostPendingDTO
            {
                Id = p.Id,
                Title = p.Title,
                Body = p.Body,
                CreatedAt = p.CreatedAt,
                UserName = p.User.UserName
            });

            return Ok(new APIResponse
            {
                StatusCode = HttpStatusCode.OK,
                Result = result
            });
        }



        /// <summary>
        /// Moderator approve bài viết cộng đồng
        /// </summary>
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApprovePost(string id)
        {
            var response = new APIResponse();
            try
            {
                var post = await _uow.CommunityPosts.GetAsync(p => p.Id == id);
                if (post == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Post not found.");
                }
                else
                {
                    post.ApprovalStatus = ApprovalStatus.Approved;
                    post.ModeratorNote = null;

                    await _uow.CommunityPosts.UpdateAsync(post);
                    await _uow.CompleteAsync();

                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = new { message = "Post approved." };
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

        /// <summary>
        /// Moderator reject bài viết cộng đồng
        /// </summary>
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectPost(string id, [FromBody] ModeratorDecisionDto dto)
        {
            var response = new APIResponse();
            try
            {
                var post = await _uow.CommunityPosts.GetAsync(p => p.Id == id);
                if (post == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Post not found.");
                }
                else
                {
                    post.ApprovalStatus = ApprovalStatus.Rejected;
                    post.ModeratorNote = dto?.Reason;

                    await _uow.CommunityPosts.UpdateAsync(post);
                    await _uow.CompleteAsync();

                    response.StatusCode = HttpStatusCode.OK;
                    response.Result = new { message = "Post rejected." };
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
        [AllowAnonymous]
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            var response = new APIResponse();

            try
            {
                var post = await _service.GetPostByIdAsync(postId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = post;
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
