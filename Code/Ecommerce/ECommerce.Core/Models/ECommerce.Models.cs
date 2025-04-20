using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public Category? Category { get; set; }
        
        // NoSQL-like storage for flexible attributes
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        
        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public required string Username { get; set; }
        
        [Required]
        public required string PasswordHash { get; set; }
        
        [Required]
        public required string Salt { get; set; }
        
        [Required]
        public required string Email { get; set; }
        
        [Required]
        public required string FirstName { get; set; }
        
        [Required]
        public required string LastName { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        
        // NoSQL-like storage for user preferences
        public Dictionary<string, object> Preferences { get; set; } = new();
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        
        // Navigation properties
        public User? User { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}