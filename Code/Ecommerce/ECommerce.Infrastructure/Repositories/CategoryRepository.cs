using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Infrastructure.Data.Repositories
{
    [SupportedOSPlatform("windows")]
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AccessDbContext _dbContext;

        public CategoryRepository(AccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            var categories = new List<Category>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Categories ORDER BY ID", 
                connection);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    categories.Add(new Category
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = reader["Name"].ToString(),
                        Description = reader["Description"].ToString()
                    });
                }
            }
            
            return categories;
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Categories WHERE ID = @Id", 
                connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new Category
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = reader["Name"].ToString(),
                        Description = reader["Description"].ToString()
                    };
                }
            }
            
            return null;
        }

        public async Task<int> CreateAsync(Category category)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"INSERT INTO Categories (Name, Description) 
                VALUES (@Name, @Description);
                SELECT @@IDENTITY", connection);
            
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"UPDATE Categories 
                SET Name = @Name, Description = @Description 
                WHERE ID = @Id", connection);
            
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description);
            command.Parameters.AddWithValue("@Id", category.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "DELETE FROM Categories WHERE ID = @Id", 
                connection);
            command.Parameters.AddWithValue("@Id", id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}