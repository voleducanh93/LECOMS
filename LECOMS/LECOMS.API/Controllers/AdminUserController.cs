using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.User;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [Route("api/admin/user")]
    [ApiController]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
    public class AdminUserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly APIResponse _response;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUserController(IUserService userService, APIResponse response, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userService = userService;
            _response = response;
            _userManager = userManager;
            _roleManager = roleManager; // ⭐ bổ sung dòng này
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            var userDtos = new List<UserDTO>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u); // chạy lần lượt
                userDtos.Add(new UserDTO
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    DateOfBirth = u.DateOfBirth,
                    IsActive = u.IsActive,
                    Role = roles.FirstOrDefault()
                });
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = userDtos;
            return Ok(_response);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("User not found");
                return NotFound(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = user;
            return Ok(_response);
        }

        [HttpPut("activate/{id}")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var result = await _userService.ActivateUser(id);
            _response.StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            _response.Result = new { Message = result ? "User activated" : "Failed to activate" };
            return Ok(_response);
        }

        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var result = await _userService.DeactivateUser(id);
            _response.StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            _response.Result = new { Message = result ? "User deactivated" : "Failed to deactivate" };
            return Ok(_response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _userService.DeleteUser(id);
            _response.StatusCode = result ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            _response.Result = new { Message = result ? "User deleted" : "Failed to delete" };
            return Ok(_response);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string keyword)
        {
            var users = await _userService.SearchUsers(keyword);
            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = users;
            return Ok(_response);
        }
        // ====================== ROLE MANAGEMENT ======================

        // Tạo Role mới (ví dụ: Moderator)
        [HttpPost("role/create")]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Role already exists");
                return BadRequest(_response);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            _response.StatusCode = result.Succeeded ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            _response.Result = new { Message = $"Role '{roleName}' created" };
            return Ok(_response);
        }

        // Lấy danh sách roles
        [HttpGet("role/all")]
        public IActionResult GetAllRoles()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(_response);
        }

        // Gán role cho user
        [HttpPost("role/assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("User not found");
                return NotFound(_response);
            }

            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Role not found");
                return NotFound(_response);
            }

            await _userManager.AddToRoleAsync(user, dto.Role);

            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = new { Message = $"Added role '{dto.Role}' to user '{user.UserName}'" };
            return Ok(_response);
        }

        // Gỡ role của user
        [HttpPost("role/remove")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("User not found");
                return NotFound(_response);
            }

            await _userManager.RemoveFromRoleAsync(user, dto.Role);

            _response.StatusCode = HttpStatusCode.OK;
            _response.Result = new { Message = $"Removed role '{dto.Role}' from '{user.UserName}'" };
            return Ok(_response);
        }

    }
}
