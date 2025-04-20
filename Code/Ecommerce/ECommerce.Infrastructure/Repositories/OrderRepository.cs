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
    public class OrderRepository : IOrderRepository
    {
        private readonly AccessDbContext _dbContext;

        public OrderRepository(AccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Order>> GetAllByUserIdAsync(int userId)
        {
            var orders = new List<Order>();
            
            var connection = _dbContext.GetConnection();
            var command = new OleDbCommand(
                "SELECT * FROM Orders WHERE UserID = @UserId ORDER BY OrderDate DESC", 
                connection);
            command.Parameters.Add("@UserId", OleDbType.Integer).Value = userId;
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var order = new Order
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        UserId = Convert.ToInt32(reader["UserID"]),
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        Status = reader["Status"].ToString() ?? string.Empty
                    };
                    
                    // Load order items
                    order.Items = await GetOrderItemsAsync(order.Id);
                    
                    orders.Add(order);
                }
            }
            
            return orders;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            Order? order = null;
            
            var connection = _dbContext.GetConnection();
            var command = new OleDbCommand("SELECT * FROM Orders WHERE ID = @Id", connection);
            command.Parameters.Add("@Id", OleDbType.Integer).Value = id;
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    order = new Order
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        UserId = Convert.ToInt32(reader["UserID"]),
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        Status = reader["Status"].ToString() ?? string.Empty
                    };
                    
                    // Load order items
                    order.Items = await GetOrderItemsAsync(order.Id);
                }
            }
            
            return order;
        }

        public async Task<int> CreateAsync(Order order)
        {
            var connection = _dbContext.GetConnection();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Insert order
                    var orderCommand = new OleDbCommand(
                        "INSERT INTO Orders (UserID, OrderDate, TotalAmount, Status) " +
                        "VALUES (@UserId, @OrderDate, @TotalAmount, @Status)",
                        connection, transaction);
                    
                    orderCommand.Parameters.Add("@UserId", OleDbType.Integer).Value = order.UserId;
                    orderCommand.Parameters.Add("@OrderDate", OleDbType.Date).Value = order.OrderDate;
                    orderCommand.Parameters.Add("@TotalAmount", OleDbType.Currency).Value = order.TotalAmount;
                    orderCommand.Parameters.Add("@Status", OleDbType.VarWChar).Value = order.Status;
                    
                    await orderCommand.ExecuteNonQueryAsync();
                    
                    // Get the last inserted ID
                    var identityCommand = new OleDbCommand(
                        "SELECT @@IDENTITY",
                        connection, transaction);
                    
                    var orderId = Convert.ToInt32(await identityCommand.ExecuteScalarAsync());
                    order.Id = orderId;
                    
                    // Insert order items
                    foreach (var item in order.Items)
                    {
                        var itemCommand = new OleDbCommand(
                            "INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPrice) " +
                            "VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)",
                            connection, transaction);
                        
                        itemCommand.Parameters.Add("@OrderId", OleDbType.Integer).Value = orderId;
                        itemCommand.Parameters.Add("@ProductId", OleDbType.Integer).Value = item.ProductId;
                        itemCommand.Parameters.Add("@Quantity", OleDbType.Integer).Value = item.Quantity;
                        itemCommand.Parameters.Add("@UnitPrice", OleDbType.Currency).Value = item.UnitPrice;
                        
                        await itemCommand.ExecuteNonQueryAsync();
                    }
                    
                    transaction.Commit();
                    return orderId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<bool> UpdateAsync(Order order)
        {
            var connection = _dbContext.GetConnection();
            var command = new OleDbCommand(
                "UPDATE Orders SET Status = @Status, TotalAmount = @TotalAmount WHERE ID = @Id",
                connection);
            
            command.Parameters.Add("@Status", OleDbType.VarWChar).Value = order.Status;
            command.Parameters.Add("@TotalAmount", OleDbType.Currency).Value = order.TotalAmount;
            command.Parameters.Add("@Id", OleDbType.Integer).Value = order.Id;
            
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _dbContext.GetConnection();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Delete order items first (foreign key constraint)
                    var itemsCommand = new OleDbCommand(
                        "DELETE FROM OrderItems WHERE OrderID = @OrderId", 
                        connection, transaction);
                    itemsCommand.Parameters.Add("@OrderId", OleDbType.Integer).Value = id;
                    await itemsCommand.ExecuteNonQueryAsync();
                    
                    // Then delete the order
                    var orderCommand = new OleDbCommand(
                        "DELETE FROM Orders WHERE ID = @Id", 
                        connection, transaction);
                    orderCommand.Parameters.Add("@Id", OleDbType.Integer).Value = id;
                    
                    int rowsAffected = await orderCommand.ExecuteNonQueryAsync();
                    
                    transaction.Commit();
                    return rowsAffected > 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        
        private async Task<ICollection<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            var items = new List<OrderItem>();
            
            var connection = _dbContext.GetConnection();
            var command = new OleDbCommand(
                "SELECT oi.*, p.Name as ProductName, p.Price as ProductPrice, p.ImageUrl as ProductImageUrl " +
                "FROM OrderItems oi " +
                "INNER JOIN Products p ON oi.ProductID = p.ID " +
                "WHERE oi.OrderID = @OrderId", 
                connection);
            command.Parameters.Add("@OrderId", OleDbType.Integer).Value = orderId;
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new OrderItem
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        OrderId = orderId,
                        ProductId = Convert.ToInt32(reader["ProductID"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                        Product = new Product
                        {
                            Id = Convert.ToInt32(reader["ProductID"]),
                            Name = reader["ProductName"].ToString() ?? string.Empty,
                            Price = Convert.ToDecimal(reader["ProductPrice"]),
                            ImageUrl = reader["ProductImageUrl"].ToString()
                        }
                    });
                }
            }
            
            return items;
        }

#if DEBUG
        public async Task ReadDatabaseSchema()
        {
            var connection = _dbContext.GetConnection();
            var schema = await Task.Run(() => connection.GetSchema("Columns"));
            foreach (System.Data.DataRow row in schema.Rows)
            {
                Console.WriteLine($"Table: {row["TABLE_NAME"]}, Column: {row["COLUMN_NAME"]}, Type: {row["DATA_TYPE"]}");
            }
        }
#endif
    }
}