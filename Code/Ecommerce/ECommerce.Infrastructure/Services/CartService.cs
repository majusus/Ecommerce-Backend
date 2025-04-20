using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public async Task<CartDto?> GetUserCartAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
            {
                // Create new cart for user
                var newCart = new Cart
                {
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    //LastModifiedDate = DateTime.UtcNow
                };
                
                var cartId = await _cartRepository.CreateAsync(newCart);
                cart = await _cartRepository.GetByUserIdAsync(userId);
                
                if (cart == null)
                {
                    throw new InvalidOperationException("Failed to create cart for user");
                }
            }

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedDate = cart.CreatedDate,
                Items = cart.Items.Select(MapToCartItemDto).ToList(),
                TotalAmount = cart.Items.Sum(i => i.Product?.Price * i.Quantity ?? 0)
            };
        }

        public async Task<CartDto> AddItemToCartAsync(int userId, AddCartItemDto addCartItemDto)
        {
            var cart = await EnsureCartExistsAsync(userId);
            var product = await _productRepository.GetByIdAsync(addCartItemDto.ProductId);
            
            if (product == null)
                throw new InvalidOperationException("Product not found");

            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Product = product,
                Quantity = addCartItemDto.Quantity
            };

            await _cartRepository.AddItemAsync(cartItem);
            
            // Reload cart to get updated state
            cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found after adding item");

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedDate = cart.CreatedDate,
                Items = cart.Items.Select(MapToCartItemDto).ToList(),
                TotalAmount = cart.Items.Sum(i => i.Product?.Price * i.Quantity ?? 0)
            };
        }

        public async Task<CartDto> UpdateCartItemAsync(int userId, UpdateCartItemDto updateCartItemDto)
        {
            var cart = await EnsureCartExistsAsync(userId);
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == updateCartItemDto.ProductId);
            
            if (cartItem == null)
                throw new InvalidOperationException("Item not found in cart");

            cartItem.Quantity = updateCartItemDto.Quantity;
            await _cartRepository.UpdateItemAsync(cartItem);
            
            // Reload cart to get updated state
            cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found after updating item");

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedDate = cart.CreatedDate,
                Items = cart.Items.Select(MapToCartItemDto).ToList(),
                TotalAmount = cart.Items.Sum(i => i.Product?.Price * i.Quantity ?? 0)
            };
        }

        public async Task<CartDto> RemoveCartItemAsync(int userId, int productId)
        {
            var cart = await EnsureCartExistsAsync(userId);
            await _cartRepository.RemoveItemAsync(cart.Id, productId);
            
            // Reload cart to get updated state
            cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found after removing item");

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedDate = cart.CreatedDate,
                Items = cart.Items.Select(MapToCartItemDto).ToList(),
                TotalAmount = cart.Items.Sum(i => i.Product?.Price * i.Quantity ?? 0)
            };
        }

        public async Task<CartDto> ClearCartAsync(int userId)
        {
            var cart = await EnsureCartExistsAsync(userId);
            await _cartRepository.DeleteAsync(cart.Id);
            
            // Create a new empty cart
            var newCart = new Cart
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                //LastModifiedDate = DateTime.UtcNow
            };
            
            var cartId = await _cartRepository.CreateAsync(newCart);
            cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                throw new InvalidOperationException("Failed to create new cart after clearing");

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedDate = cart.CreatedDate,
                Items = new List<CartItemDto>(),
                TotalAmount = 0
            };
        }

        private CartItemDto MapToCartItemDto(CartItem cartItem)
        {
            if (cartItem.Product == null)
                throw new InvalidOperationException("Product not loaded for cart item");

            return new CartItemDto
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Name ?? string.Empty,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Product.Price,
                ProductImageUrl = cartItem.Product.ImageUrl ?? string.Empty,
                Subtotal = cartItem.Product.Price * cartItem.Quantity
            };
        }

        private async Task<Cart> EnsureCartExistsAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
            {
                var newCart = new Cart
                {
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    //LastModifiedDate = DateTime.UtcNow
                };
                
                var cartId = await _cartRepository.CreateAsync(newCart);
                cart = await _cartRepository.GetByUserIdAsync(userId);
                
                if (cart == null)
                {
                    throw new InvalidOperationException("Failed to create cart for user");
                }
            }
            return cart;
        }
    }
}