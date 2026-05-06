using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodiGOAPI.Data;
using FoodiGOAPI.Models;
using FoodiGOAPI.DTO;

namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Get user's cart
        [HttpGet]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                var cartItems = await _context.Cart
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .Select(c => new CartItemDto
                    {
                        Id = c.Id,
                        ProductId = c.ProductId,
                        ProductName = c.Product!.Name,
                        Price = c.Product!.Price,
                        Quantity = c.Quantity,
                        ImagePath = c.Product.ImagePath
                    })
                    .ToListAsync();

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ Add item to cart
        [HttpPost("add")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                // Check if product exists
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null) return NotFound("Product not found");

                // Check if item already in cart
                var cartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);

                if (cartItem != null)
                {
                    // Update quantity
                    cartItem.Quantity += dto.Quantity;
                }
                else
                {
                    // Add new item
                    cartItem = new Cart
                    {
                        UserId = userId,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity
                    };
                    _context.Cart.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Item added to cart" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ Update cart item quantity
        [HttpPut("{id}")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                var cartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (cartItem == null) return NotFound("Cart item not found");

                if (dto.Quantity <= 0)
                {
                    _context.Cart.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = dto.Quantity;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cart updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ Remove item from cart
        [HttpDelete("{id}")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                var cartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (cartItem == null) return NotFound("Cart item not found");

                _context.Cart.Remove(cartItem);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Item removed from cart" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ Clear cart
        [HttpDelete("clear")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                var cartItems = await _context.Cart
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    _context.Cart.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Cart cleared" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}