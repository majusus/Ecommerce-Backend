using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;

namespace ECommerce.Core.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<OrderDto> CreateOrderFromCartAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
    }
}