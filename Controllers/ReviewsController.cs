using FoodiGOAPI.Data;
using FoodiGOAPI.DTO;
using FoodiGOAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Get reviews for specific product (Public)
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && !r.IsDeleted)
                .Select(r => new
                {
                    r.Id,
                    r.ProductId,
                    r.UserId,
                    UserName = r.User!.FullName,
                    r.Rating,
                    r.Comment,
                    r.AdminReply,
                    r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        // ✅ Get ALL reviews (Admin only)
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => !r.IsDeleted)
                .Select(r => new
                {
                    r.Id,
                    r.ProductId,
                    ProductName = r.Product!.Name,
                    r.UserId,
                    UserName = r.User!.FullName,
                    r.Rating,
                    r.Comment,
                    r.AdminReply,
                    r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        // ✅ User adds review
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(CreateReviewDto dto)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return BadRequest("User ID not found in token");

            if (!int.TryParse(userIdClaim.Value, out var userId))
                return BadRequest("Invalid user ID in token");

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(review);
        }

        // ✅ Admin reply
        [HttpPut("{id}/reply")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Reply(int id, ReplyReviewDto dto)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.AdminReply = dto.Reply;
            await _context.SaveChangesAsync();
            return Ok(review);
        }

        // ✅ Admin delete (soft delete)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}