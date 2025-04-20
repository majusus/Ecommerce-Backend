using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Newtonsoft.Json;

namespace ECommerce.Infrastructure.Data.Repositories
{
    [SupportedOSPlatform("windows")]
    public class UserRepository : IUserRepository
    {
        private readonly AccessDbContext _dbContext;

        public UserRepository(AccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var users = new List<User>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Users ORDER BY ID", 
                connection);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Username = reader["Username"]?.ToString() ?? string.Empty,
                        PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                        Salt = reader["Salt"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                        LastName = reader["LastName"]?.ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load user preferences
                    user.Preferences = await GetUserPreferencesAsync(user.Id);
                    users.Add(user);
                }
            }
            
            return users;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Users WHERE ID = @Id", 
                connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Username = reader["Username"]?.ToString() ?? string.Empty,
                        PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                        Salt = reader["Salt"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                        LastName = reader["LastName"]?.ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load user preferences
                    user.Preferences = await GetUserPreferencesAsync(user.Id);
                    return user;
                }
            }
            
            return null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Users WHERE Username = @Username", 
                connection);
            command.Parameters.AddWithValue("@Username", username);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Username = reader["Username"]?.ToString() ?? string.Empty,
                        PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                        Salt = reader["Salt"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                        LastName = reader["LastName"]?.ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load user preferences
                    user.Preferences = await GetUserPreferencesAsync(user.Id);
                    return user;
                }
            }
            
            return null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Users WHERE Email = @Email", 
                connection);
            command.Parameters.AddWithValue("@Email", email);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Username = reader["Username"]?.ToString() ?? string.Empty,
                        PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                        Salt = reader["Salt"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                        LastName = reader["LastName"]?.ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load user preferences
                    user.Preferences = await GetUserPreferencesAsync(user.Id);
                    return user;
                }
            }
            
            return null;
        }

        public async Task<int> CreateAsync(User user)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"INSERT INTO Users (Username, PasswordHash, Salt, Email, FirstName, LastName, CreatedDate) 
                VALUES (@Username, @PasswordHash, @Salt, @Email, @FirstName, @LastName, @CreatedDate);
                SELECT @@IDENTITY", connection);
            
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@Salt", user.Salt);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
            
            var result = await command.ExecuteScalarAsync();
            var userId = Convert.ToInt32(result);
            
            // Save user preferences if present
            if (user.Preferences != null && user.Preferences.Count > 0)
            {
                await SaveUserPreferencesAsync(userId, user.Preferences);
            }
            
            return userId;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"UPDATE Users 
                SET Email = @Email, FirstName = @FirstName, LastName = @LastName 
                WHERE ID = @Id", connection);
            
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);
            command.Parameters.AddWithValue("@Id", user.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            // Update user preferences if present
            if (rowsAffected > 0 && user.Preferences != null)
            {
                await SaveUserPreferencesAsync(user.Id, user.Preferences);
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            // First delete user preferences
            var preferencesCommand = new OleDbCommand(
                "DELETE FROM UserPreferences WHERE UserID = @Id", 
                connection);
            preferencesCommand.Parameters.AddWithValue("@Id", id);
            await preferencesCommand.ExecuteNonQueryAsync();
            
            // Then delete the user
            var command = new OleDbCommand(
                "DELETE FROM Users WHERE ID = @Id", 
                connection);
            command.Parameters.AddWithValue("@Id", id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        
        private async Task<Dictionary<string, object>> GetUserPreferencesAsync(int userId)
        {
            var preferences = new Dictionary<string, object>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"SELECT PreferencesJSON 
                FROM UserPreferences 
                WHERE UserID = @UserId", 
                connection);
            command.Parameters.AddWithValue("@UserId", userId);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var jsonPreferences = reader["PreferencesJSON"]?.ToString();
                    if (!string.IsNullOrEmpty(jsonPreferences))
                    {
                        preferences = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonPreferences) 
                            ?? new Dictionary<string, object>();
                    }
                }
            }
            
            return preferences;
        }
        
        private async Task SaveUserPreferencesAsync(int userId, Dictionary<string, object> preferences)
        {
            var connection = _dbContext.GetConnection();
            
            var jsonPreferences = JsonConvert.SerializeObject(preferences);
            
            // Check if preferences exist for the user
            var checkCommand = new OleDbCommand(
                "SELECT COUNT(*) FROM UserPreferences WHERE UserID = @UserId",
                connection);
            checkCommand.Parameters.AddWithValue("@UserId", userId);
            var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            if (exists)
            {
                // Update existing preferences
                var updateCommand = new OleDbCommand(
                    @"UPDATE UserPreferences 
                    SET PreferencesJSON = @PreferencesJSON 
                    WHERE UserID = @UserId",
                    connection);
                updateCommand.Parameters.AddWithValue("@PreferencesJSON", jsonPreferences);
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                await updateCommand.ExecuteNonQueryAsync();
            }
            else
            {
                // Insert new preferences
                var insertCommand = new OleDbCommand(
                    @"INSERT INTO UserPreferences (UserID, PreferencesJSON) 
                    VALUES (@UserId, @PreferencesJSON)",
                    connection);
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.Parameters.AddWithValue("@PreferencesJSON", jsonPreferences);
                await insertCommand.ExecuteNonQueryAsync();
            }
        }
    }
}