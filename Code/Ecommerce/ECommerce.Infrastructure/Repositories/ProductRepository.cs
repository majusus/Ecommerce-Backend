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
    public class ProductRepository : IProductRepository
    {
        private readonly AccessDbContext _dbContext;

        public ProductRepository(AccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var products = new List<Product>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"SELECT * FROM Products 
                ORDER BY ID 
                OFFSET @Offset ROWS 
                FETCH NEXT @PageSize ROWS ONLY", connection);
                
            command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var product = new Product
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = reader["Name"].ToString(),
                        Description = reader["Description"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        CategoryId = Convert.ToInt32(reader["CategoryID"]),
                        StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load product attributes
                    product.Attributes = await GetProductAttributesAsync(product.Id);
                    products.Add(product);
                }
            }
            
            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand("SELECT * FROM Products WHERE ID = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var product = new Product
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = reader["Name"].ToString(),
                        Description = reader["Description"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        CategoryId = Convert.ToInt32(reader["CategoryID"]),
                        StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    
                    // Load product attributes
                    product.Attributes = await GetProductAttributesAsync(product.Id);
                    return product;
                }
            }
            
            return null;
        }

        public async Task<int> CreateAsync(Product product)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"INSERT INTO Products (Name, Description, Price, CategoryID, StockQuantity, CreatedDate) 
                VALUES (@Name, @Description, @Price, @CategoryId, @StockQuantity, @CreatedDate);
                SELECT @@IDENTITY", connection);
            
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
            
            var result = await command.ExecuteScalarAsync();
            var productId = Convert.ToInt32(result);
            
            // Save product attributes
            if (product.Attributes != null && product.Attributes.Count > 0)
            {
                await SaveProductAttributesAsync(productId, product.Attributes);
            }
            
            return productId;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"UPDATE Products 
                SET Name = @Name, Description = @Description, Price = @Price, 
                    CategoryID = @CategoryId, StockQuantity = @StockQuantity 
                WHERE ID = @Id", connection);
            
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@Id", product.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            // Update product attributes
            if (rowsAffected > 0 && product.Attributes != null)
            {
                await SaveProductAttributesAsync(product.Id, product.Attributes);
            }
            
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            // First delete product attributes
            var attributesCommand = new OleDbCommand(
                "DELETE FROM ProductAttributes WHERE ProductID = @Id", connection);
            attributesCommand.Parameters.AddWithValue("@Id", id);
            await attributesCommand.ExecuteNonQueryAsync();
            
            // Then delete the product
            var command = new OleDbCommand("DELETE FROM Products WHERE ID = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        
        private async Task<Dictionary<string, object>> GetProductAttributesAsync(int productId)
        {
            var attributes = new Dictionary<string, object>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT AttributeName, AttributeValue FROM ProductAttributes WHERE ProductID = @ProductId", 
                connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    attributes[reader["AttributeName"].ToString()] = 
                        JsonConvert.DeserializeObject(reader["AttributeValue"].ToString());
                }
            }
            
            return attributes;
        }
        
        private async Task SaveProductAttributesAsync(int productId, Dictionary<string, object> attributes)
        {
            var connection = _dbContext.GetConnection();
            
            // First delete existing attributes
            var deleteCommand = new OleDbCommand(
                "DELETE FROM ProductAttributes WHERE ProductID = @ProductId", connection);
            deleteCommand.Parameters.AddWithValue("@ProductId", productId);
            await deleteCommand.ExecuteNonQueryAsync();
            
            // Then insert new attributes
            foreach (var attribute in attributes)
            {
                var command = new OleDbCommand(
                    @"INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue) 
                    VALUES (@ProductId, @Name, @Value)", connection);
                
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@Name", attribute.Key);
                command.Parameters.AddWithValue("@Value", JsonConvert.SerializeObject(attribute.Value));
                
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}