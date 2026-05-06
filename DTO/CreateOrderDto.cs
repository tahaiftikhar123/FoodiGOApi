using System.ComponentModel.DataAnnotations;

namespace FoodiGOAPI.DTO
{
    // ==================== CREATE ORDER REQUEST ====================
    public class CreateOrderDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Street { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = "Cash On Delivery"; // Cash On Delivery or Online Payment
    }

    // ==================== ORDER RESPONSE ====================
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal DeliveryCharges { get; set; }
        public decimal TotalAmount { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty; // In Progress, Out For Delivery, Delivered
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty; // Pending, Completed, Failed

        public List<OrderItemDto> OrderItems { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    // ==================== ORDER ITEM RESPONSE ====================
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==================== UPDATE ORDER STATUS (ADMIN) ====================
    public class UpdateOrderStatusDto
    {
        [Required]
        public string OrderStatus { get; set; } = string.Empty; // In Progress, Out For Delivery, Delivered
    }

    // ==================== PAGINATION HELPER ====================
    public class PaginatedOrdersDto
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}