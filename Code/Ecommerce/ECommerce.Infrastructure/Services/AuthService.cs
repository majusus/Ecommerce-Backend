using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtConfig _jwtConfig;
        private readonly string _bearer = $@"Bearer ";

        public AuthService(IUserRepository userRepository, IOptions<JwtConfig> jwtConfig)
        {
            _userRepository = userRepository;
            _jwtConfig = jwtConfig.Value;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsernameAsync(loginDto.Username);
            
            if (user == null)
            {
                return null;
            }
            
            if (!VerifyPassword(user.PasswordHash, loginDto.Password, user.Salt))
            {
                return null;
            }
            
            // Generate JWT token
            string token = GenerateJwtToken(user);
            
            return new AuthResponseDto
            {
                Token = _bearer + token,
                Expiration = DateTime.Now.AddMinutes(_jwtConfig.ExpirationInMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedDate = user.CreatedDate
                }
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerUserDto)
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
                CreatedDate = DateTime.Now,
                Preferences = new Dictionary<string, object>()
            };
            
            var userId = await _userRepository.CreateAsync(user);
            user.Id = userId;
            
            // Generate JWT token
            string token = GenerateJwtToken(user);
            
            return new AuthResponseDto
            {
                Token = _bearer + token,
                Expiration = DateTime.Now.AddMinutes(_jwtConfig.ExpirationInMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedDate = user.CreatedDate
                }
            };
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(_jwtConfig.ExpirationInMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool VerifyPassword(string storedHash, string password, string salt)
        {
            bool isPasswordValid = PasswordHelper.VerifyPassword(password, storedHash, salt);
            return isPasswordValid;
        }
    }
}