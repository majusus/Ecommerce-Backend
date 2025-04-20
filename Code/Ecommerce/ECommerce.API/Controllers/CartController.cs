using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _cartService.GetUserCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("items")]
        public async Task<ActionResult<CartDto>> AddItem(AddCartItemDto addCartItemDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _cartService.AddItemToCartAsync(userId, addCartItemDto);
            return Ok(cart);
        }

        [HttpPut("items")]
        public async Task<ActionResult<CartDto>> UpdateItem(UpdateCartItemDto updateCartItemDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _cartService.UpdateCartItemAsync(userId, updateCartItemDto);
            return Ok(cart);
        }

        [HttpDelete("items/{productId}")]
        public async Task<ActionResult<CartDto>> RemoveItem(int productId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _cartService.RemoveCartItemAsync(userId, productId);
            return Ok(cart);
        }

        [HttpDelete]
        public async Task<ActionResult<CartDto>> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var cart = await _cartService.ClearCartAsync(userId);
            return Ok(cart);
        }
    }
}