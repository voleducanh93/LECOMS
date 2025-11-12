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
        public AdminUserController(IUserService userService, APIResponse response, UserManager<User> userManager)
        {
            _userService = userService;
            _response = response;
            _userManager = userManager;
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
    }
}
