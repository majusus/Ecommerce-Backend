using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Infrastructure.Data
{
    [SupportedOSPlatform("windows")]
    public class CartRepository : ICartRepository
    {
        private readonly AccessDbContext _dbContext;

        public CartRepository(AccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                "SELECT * FROM Carts WHERE UserID = @UserId", 
                connection);
            command.Parameters.AddWithValue("@UserId", userId);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var cart = new Cart
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        UserId = userId,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        //LastModifiedDate = Convert.ToDateTime(reader["LastModifiedDate"])
                    };
                    
                    // Load cart items
                    cart.Items = await GetCartItemsAsync(cart.Id);
                    return cart;
                }
            }
            
            return null;
        }

        public async Task<int> CreateAsync(Cart cart)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"INSERT INTO Carts (UserID, CreatedDate, LastModifiedDate) 
                VALUES (@UserId, @CreatedDate, @LastModifiedDate);
                SELECT @@IDENTITY", connection);
            
            command.Parameters.AddWithValue("@UserId", cart.UserId);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
            command.Parameters.AddWithValue("@LastModifiedDate", DateTime.UtcNow);
            
            var result = await command.ExecuteScalarAsync();
            var cartId = Convert.ToInt32(result);
            
            // Save cart items if present
            if (cart.Items != null && cart.Items.Count > 0)
            {
                foreach (var item in cart.Items)
                {
                    item.CartId = cartId;
                    await AddItemAsync(item);
                }
            }
            
            return cartId;
        }

        public async Task<bool> UpdateAsync(Cart cart)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"UPDATE Carts 
                SET LastModifiedDate = @LastModifiedDate 
                WHERE ID = @Id", connection);
            
            command.Parameters.AddWithValue("@LastModifiedDate", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", cart.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            
            // First delete cart items
            var itemsCommand = new OleDbCommand(
                "DELETE FROM CartItems WHERE CartID = @Id", connection);
            itemsCommand.Parameters.AddWithValue("@Id", id);
            await itemsCommand.ExecuteNonQueryAsync();
            
            // Then delete the cart
            var command = new OleDbCommand("DELETE FROM Carts WHERE ID = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> AddItemAsync(CartItem item)
        {
            var connection = _dbContext.GetConnection();
            
            // Check if item already exists
            var existingItem = await GetCartItemAsync(item.CartId, item.ProductId);
            if (existingItem != null)
            {
                // Update quantity instead of adding new item
                var updateCommand = new OleDbCommand(
                    @"UPDATE CartItems 
                    SET Quantity = Quantity + @Quantity 
                    WHERE CartID = @CartId AND ProductID = @ProductId", 
                    connection);
                
                updateCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                updateCommand.Parameters.AddWithValue("@CartId", item.CartId);
                updateCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            
            // Add new item
            var command = new OleDbCommand(
                @"INSERT INTO CartItems (CartID, ProductID, Quantity) 
                VALUES (@CartId, @ProductId, @Quantity)", connection);
            
            command.Parameters.AddWithValue("@CartId", item.CartId);
            command.Parameters.AddWithValue("@ProductId", item.ProductId);
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            
            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> UpdateItemAsync(CartItem item)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"UPDATE CartItems 
                SET Quantity = @Quantity 
                WHERE CartID = @CartId AND ProductID = @ProductId", 
                connection);
            
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            command.Parameters.AddWithValue("@CartId", item.CartId);
            command.Parameters.AddWithValue("@ProductId", item.ProductId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> RemoveItemAsync(int cartId, int productId)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"DELETE FROM CartItems 
                WHERE CartID = @CartId AND ProductID = @ProductId", 
                connection);
            
            command.Parameters.AddWithValue("@CartId", cartId);
            command.Parameters.AddWithValue("@ProductId", productId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        
        private async Task<ICollection<CartItem>> GetCartItemsAsync(int cartId)
        {
            var items = new List<CartItem>();
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"SELECT * FROM CartItems WHERE CartID = @CartId", 
                connection);
            command.Parameters.AddWithValue("@CartId", cartId);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new CartItem
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        CartId = cartId,
                        ProductId = Convert.ToInt32(reader["ProductID"]),
                        Quantity = Convert.ToInt32(reader["Quantity"])
                    });
                }
            }
            
            return items;
        }
        
        private async Task<CartItem?> GetCartItemAsync(int cartId, int productId)
        {
            var connection = _dbContext.GetConnection();
            
            var command = new OleDbCommand(
                @"SELECT * FROM CartItems 
                WHERE CartID = @CartId AND ProductID = @ProductId", 
                connection);
            
            command.Parameters.AddWithValue("@CartId", cartId);
            command.Parameters.AddWithValue("@ProductId", productId);
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new CartItem
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        CartId = cartId,
                        ProductId = productId,
                        Quantity = Convert.ToInt32(reader["Quantity"])
                    };
                }
            }
            
            return null;
        }
    }
}