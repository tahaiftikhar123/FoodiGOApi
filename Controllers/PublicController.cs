using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodiGOAPI.Data;

namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PublicController(AppDbContext context) => _context = context;

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() => Ok(await _context.Categories.ToListAsync());

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts() => Ok(await _context.Products.Include(p => p.Category).ToListAsync());

        [HttpGet("products/category/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId) =>
            Ok(await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync());
    }
}