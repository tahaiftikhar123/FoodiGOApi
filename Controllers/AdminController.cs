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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CategoriesController(AppDbContext context) => _context = context;

        // PUBLIC - Anyone can view (no auth required)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            => Ok(await _context.Categories.ToListAsync());

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
            => Ok(await _context.Categories.FindAsync(id));

        // ADMIN ONLY - Create, Update, Delete
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(CategoryDto dto)
        {
            var category = new Category { Name = dto.Name, Description = dto.Description, ImagePath = dto.ImagePath };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, CategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ImagePath = dto.ImagePath;
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context) => _context = context;

        // PUBLIC - Anyone can view
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            => Ok(await _context.Products.Include(p => p.Category).ToListAsync());

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
            => Ok(await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id));

        // ADMIN ONLY - Create, Update, Delete
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                ImagePath = dto.ImagePath,
                CategoryId = dto.CategoryId
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.ImagePath = dto.ImagePath;
            product.CategoryId = dto.CategoryId;
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}