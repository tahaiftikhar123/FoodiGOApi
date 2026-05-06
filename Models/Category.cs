using System.ComponentModel.DataAnnotations;

namespace FoodiGOAPI.Models;
public class Category
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImagePath { get; set; }  // ✅ Matches DB
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}