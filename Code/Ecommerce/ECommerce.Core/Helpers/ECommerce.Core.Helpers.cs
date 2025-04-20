using System;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Core.Helpers
{
    public static class PasswordHelper
    {
        public static (string hash, string salt) HashPassword(string password)
        {
            // Generate a random salt
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            
            string salt = Convert.ToBase64String(saltBytes);
            
            // Hash the password with the salt
            string hash = ComputeHash(password, salt);
            
            return (hash, salt);
        }
        
        public static bool VerifyPassword(string password, string hash, string salt)
        {
            string computedHash = ComputeHash(password, salt);
            return computedHash == hash;
        }
        
        private static string ComputeHash(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
    
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        
        public static bool IsValid(string status)
        {
            return status == Pending || 
                   status == Processing || 
                   status == Shipped || 
                   status == Delivered || 
                   status == Cancelled;
        }
    }
}