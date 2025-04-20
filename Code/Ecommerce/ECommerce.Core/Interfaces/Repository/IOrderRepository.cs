using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.Models;

namespace ECommerce.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllByUserIdAsync(int userId);
        Task<Order?> GetByIdAsync(int id);
        Task<int> CreateAsync(Order order);
        Task<bool> UpdateAsync(Order order);
        Task<bool> DeleteAsync(int id);
    }
}