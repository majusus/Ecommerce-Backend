using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using System;
using System.Threading.Tasks;
using ECommerce.Core.Helpers;

namespace ECommerce.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<UserDto> RegisterUserAsync(RegisterUserDto registerUserDto)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetByUsernameAsync(registerUserDto.Username);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Check if email already exists
            var existingEmail = await _userRepository.GetByEmailAsync(registerUserDto.Email);
            if (existingEmail != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Hash password
            var (hash, salt) = PasswordHelper.HashPassword(registerUserDto.Password);

            var user = new User
            {
                Username = registerUserDto.Username,
                Email = registerUserDto.Email,
                PasswordHash = hash,
                Salt = salt,
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName,
                CreatedDate = DateTime.UtcNow,
                Preferences = new System.Collections.Generic.Dictionary<string, object>()
            };

            var userId = await _userRepository.CreateAsync(user);
            user.Id = userId;

            return MapToUserDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Check email uniqueness if it's being changed
            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var existingEmail = await _userRepository.GetByEmailAsync(updateUserDto.Email);
                if (existingEmail != null && existingEmail.Id != id)
                {
                    throw new InvalidOperationException("Email already exists");
                }
                user.Email = updateUserDto.Email;
            }

            // Update user properties
            user.FirstName = updateUserDto.FirstName ?? user.FirstName;
            user.LastName = updateUserDto.LastName ?? user.LastName;

            await _userRepository.UpdateAsync(user);
            return MapToUserDto(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedDate = user.CreatedDate
            };
        }
    }
}