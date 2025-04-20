using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using ECommerce.Core.Models;
using ECommerce.Core.Interfaces;
using System.Linq;

namespace ECommerce.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
            
            var sender = new SmtpSender(() => new SmtpClient(_emailSettings.Host, _emailSettings.Port)
            {                
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = true,
            });

            Email.DefaultSender = sender;
        }

        public async Task SendOrderConfirmationEmail(Order order, string userEmail)
        {
            var email = Email
                .From(_emailSettings.FromEmail, _emailSettings.FromName)
                .To(userEmail)
                .Subject($"Order Confirmation - Order #{order.Id}")
                .UsingTemplate(GetOrderConfirmationTemplate(order), order);

            await email.SendAsync();
        }

        private string GetOrderConfirmationTemplate(Order order)
        {
            return $@"
                <h2>Thank you for your order!</h2>
                <p>Your order #{order.Id} has been confirmed.</p>
                <h3>Order Details:</h3>
                <table>
                    <tr>
                        <th>Product</th>
                        <th>Quantity</th>
                        <th>Price</th>
                    </tr>
                    {string.Join("", order.Items.Select(item => $@"
                        <tr>
                            <td>{item.Product?.Name ?? "Unknown Product"}</td>
                            <td>{item.Quantity}</td>
                            <td>${item.UnitPrice:F2}</td>
                        </tr>
                    "))}
                </table>
                <p><strong>Total Amount: ${order.TotalAmount:F2}</strong></p>
                <p>We will notify you when your order ships.</p>";
        }
    }
}