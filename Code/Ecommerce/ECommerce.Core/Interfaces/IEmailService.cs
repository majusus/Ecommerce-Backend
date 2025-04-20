using System.Threading.Tasks;
using ECommerce.Core.Models;

namespace ECommerce.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmail(Order order, string userEmail);
    }
}