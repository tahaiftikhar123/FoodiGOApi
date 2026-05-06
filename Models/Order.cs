using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodiGOAPI.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty; // e.g., "ORD-20250506-00001"

        // Order Summary
        [Required]
        public decimal SubTotal { get; set; } // Sum of all items

        [Required]
        public decimal DeliveryCharges { get; set; } = 0; // Extra charges for COD

        [Required]
        public decimal TotalAmount { get; set; } // SubTotal + DeliveryCharges

        // Delivery Address
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        // Order Status
        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "In Progress"; // In Progress, Out For Delivery, Delivered

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash On Delivery"; // Cash On Delivery, Online Payment

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Failed

        // Order Items (One-to-Many relationship)
        public List<OrderItem> OrderItems { get; set; } = new();

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal TotalPrice { get; set; } // Price * Quantity

        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}