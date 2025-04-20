using System.Threading.Tasks;
using ECommerce.Core.Models;

namespace ECommerce.Core.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<int> CreateAsync(Cart cart);
        Task<bool> UpdateAsync(Cart cart);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddItemAsync(CartItem item);
        Task<bool> UpdateItemAsync(CartItem item);
        Task<bool> RemoveItemAsync(int cartId, int productId);
    }
}