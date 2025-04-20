using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.Models;

namespace ECommerce.Core.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<Product?> GetByIdAsync(int id);
        Task<int> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}