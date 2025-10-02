﻿using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.User;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsers();
        Task<User> GetUserById(string id);
        Task<bool> CreateUser(User user);
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(string id);
        Task<bool> ActivateUser(string id);
        Task<bool> DeactivateUser(string id);
        Task<IEnumerable<User>> SearchUsers(string keyword);
        Task<UserProfileDTO> GetProfileAsync(string userId);
        Task<bool> UpdateProfileAsync(UserProfileDTO userDTO);
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<(bool Success, string? Message, List<string>? Errors)> CreateUserAsync(RegisterAccountDTO model);

    }
}
