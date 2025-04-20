using System.Threading.Tasks;
using ECommerce.Core.DTOs;

namespace ECommerce.Core.Interfaces
{
    public interface ICartService
    {
        Task<CartDto?> GetUserCartAsync(int userId);
        Task<CartDto> AddItemToCartAsync(int userId, AddCartItemDto addCartItemDto);
        Task<CartDto> UpdateCartItemAsync(int userId, UpdateCartItemDto updateCartItemDto);
        Task<CartDto> RemoveCartItemAsync(int userId, int productId);
        Task<CartDto> ClearCartAsync(int userId);
    }
}