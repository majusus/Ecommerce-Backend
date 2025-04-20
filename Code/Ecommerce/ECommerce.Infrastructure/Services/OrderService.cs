using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartService _cartService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public OrderService(
            IOrderRepository orderRepository, 
            ICartService cartService,
            IEmailService emailService,
            IUserService userService)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _emailService = emailService;
            _userService = userService;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetAllByUserIdAsync(userId);
            return orders.Select(MapToOrderDto);
        }

        public async Task<OrderDto> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            return order != null ? MapToOrderDto(order) : null;
        }

        public async Task<OrderDto> CreateOrderFromCartAsync(int userId)
        {
            var cart = await _cartService.GetUserCartAsync(userId);
            if (cart?.Items == null || !cart.Items.Any())
            {
                throw new InvalidOperationException("Cart is empty");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            var orderId = await _orderRepository.CreateAsync(order);
            order.Id = orderId;

            // Clear the cart after creating the order
            await _cartService.ClearCartAsync(userId);

            // Get user email and send confirmation
            var user = await _userService.GetUserByIdAsync(userId);
            if (user?.Email != null)
            {
                await _emailService.SendOrderConfirmationEmail(order, user.Email);
            }

            return MapToOrderDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return false;

            order.Status = status;
            return await _orderRepository.UpdateAsync(order);
        }

        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Quantity * i.UnitPrice
                }).ToList()
            };
        }
    }
}