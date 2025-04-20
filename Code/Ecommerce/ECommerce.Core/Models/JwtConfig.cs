namespace ECommerce.Core.Models
{
    public class JwtConfig
    {
        public required string Secret { get; set; }
        public int ExpirationInMinutes { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
    }
}