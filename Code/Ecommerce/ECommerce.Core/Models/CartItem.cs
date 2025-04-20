using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        [Required]
        public int CartId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        // Navigation properties
        public Cart? Cart { get; set; }
        public Product? Product { get; set; }
    }
}