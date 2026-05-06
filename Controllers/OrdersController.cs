using FoodiGOAPI.Data;
using FoodiGOAPI.DTO;
using FoodiGOAPI.Models;
using FoodiGOAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;


namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _config;
        private const decimal COD_DELIVERY_CHARGE = 50;
        private const decimal ONLINE_DELIVERY_CHARGE = 30;

        public OrdersController(AppDbContext context, IStripeService stripeService, IConfiguration config)
        {
            _context = context;
            _stripeService = stripeService;
            _config = config;
        }

        // ================== CREATE ORDER (Cash on Delivery) ==================
        [HttpPost("create")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest(new { error = "User not found in token" });
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest(new { error = "Invalid user ID" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return BadRequest(new { error = "User does not exist" });

                var cartItems = await _context.Cart
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any()) return BadRequest(new { error = "Your cart is empty" });

                var orderItems = new List<OrderItem>();
                decimal subTotal = 0;

                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product == null)
                        return BadRequest(new { error = $"Product ID {cartItem.ProductId} no longer exists" });

                    var product = cartItem.Product;
                    var itemTotal = product.Price * cartItem.Quantity;
                    subTotal += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = cartItem.Quantity,
                        TotalPrice = itemTotal,
                        ImagePath = product.ImagePath
                    });
                }

                var deliveryCharges = dto.PaymentMethod == "Cash On Delivery" ? COD_DELIVERY_CHARGE : ONLINE_DELIVERY_CHARGE;
                var totalAmount = subTotal + deliveryCharges;

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = GenerateOrderNumber(),
                    SubTotal = subTotal,
                    DeliveryCharges = deliveryCharges,
                    TotalAmount = totalAmount,
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    Street = dto.Street,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    OrderStatus = "In Progress",
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = "Pending",
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                _context.Cart.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                var savedOrder = await _context.Orders.Include(o => o.OrderItems).FirstAsync(o => o.Id == order.Id);
                return Ok(new { message = "Order created successfully", order = MapToOrderDto(savedOrder) });
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                Console.WriteLine($"DB Error: {inner}");
                return BadRequest(new { error = $"Database error: {inner}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        // ================== CREATE ORDER WITH ONLINE PAYMENT (Stripe) ==================
        [HttpPost("create-online-payment")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CreateOrderWithOnlinePayment([FromBody] CreateOrderDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return BadRequest("User not found in token");
                if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return BadRequest("User does not exist");

                var cartItems = await _context.Cart
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any()) return BadRequest("Your cart is empty");

                var orderItems = new List<OrderItem>();
                decimal subTotal = 0;

                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product == null)
                        return BadRequest($"Product ID {cartItem.ProductId} no longer exists");

                    var product = cartItem.Product;
                    var itemTotal = product.Price * cartItem.Quantity;
                    subTotal += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = cartItem.Quantity,
                        TotalPrice = itemTotal,
                        ImagePath = product.ImagePath
                    });
                }

                decimal deliveryCharges = ONLINE_DELIVERY_CHARGE; // 30 PKR for online
                decimal totalAmount = subTotal + deliveryCharges;
                var orderNumber = GenerateOrderNumber();

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    SubTotal = subTotal,
                    DeliveryCharges = deliveryCharges,
                    TotalAmount = totalAmount,
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    Street = dto.Street,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    OrderStatus = "Pending",
                    PaymentMethod = "Online Payment",
                    PaymentStatus = "Pending",
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
               // _context.Cart.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                var frontendBaseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
                var successUrl = $"{frontendBaseUrl}/payment-success?orderId={order.Id}";
                var cancelUrl = $"{frontendBaseUrl}/payment-cancel";

                var stripeUrl = await _stripeService.CreateCheckoutSessionAsync(
                    totalAmount, "pkr", order.Id, successUrl, cancelUrl);

                return Ok(new StripeSessionResponseDto
                {
                    CheckoutUrl = stripeUrl,
                    OrderId = order.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ================== WEBHOOK for Stripe ==================
        [HttpPost("stripe-webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            try
            {
                var webhookSecret = _config["Stripe:WebhookSecret"];
                // If no webhook secret is configured (development), skip verification
                Event stripeEvent;
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    // For testing only – skip signature verification
                    stripeEvent = EventUtility.ParseEvent(json);
                }
                else
                {
                    stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
                }

                // ✅ Use the string literal – works with any Stripe.net version
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null && session.Metadata.TryGetValue("orderId", out var orderIdStr))
                    {
                        if (int.TryParse(orderIdStr, out var orderId))
                        {
                            var order = await _context.Orders.FindAsync(orderId);
                            if (order != null)
                            {
                                order.PaymentStatus = "Completed";
                                order.OrderStatus = "In Progress";

                                // ✅ Clear the user's cart after successful payment
                                var cartItems = await _context.Cart.Where(c => c.UserId == order.UserId).ToListAsync();
                                _context.Cart.RemoveRange(cartItems);

                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook error: {ex.Message}");
                return BadRequest();
            }
        }

        // ================== OTHER ENDPOINTS (unchanged) ==================
        [HttpGet("my-orders")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetUserOrders(int pageNumber = 1, int pageSize = 10)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return BadRequest("User not found");
            if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

            var totalCount = await _context.Orders.Where(o => o.UserId == userId).CountAsync();
            var orders = await _context.Orders.Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { orders = orders.Select(MapToOrderDto), totalCount, pageNumber, pageSize });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return BadRequest("User not found");
            if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");

            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound("Order not found");
            var isAdmin = User.IsInRole("admin");
            if (!isAdmin && order.UserId != userId) return Forbid();
            return Ok(MapToOrderDto(order));
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            var validStatuses = new[] { "In Progress", "Out For Delivery", "Delivered" };
            if (!validStatuses.Contains(dto.OrderStatus)) return BadRequest("Invalid status");
            order.OrderStatus = dto.OrderStatus;
            order.UpdatedAt = DateTime.UtcNow;
            if (dto.OrderStatus == "Delivered" && order.DeliveredAt == null)
            {
                order.DeliveredAt = DateTime.UtcNow;
                if (order.PaymentMethod == "Cash On Delivery") order.PaymentStatus = "Completed";
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Order status updated", order = MapToOrderDto(order) });
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllOrders(string? orderStatus = null, string? paymentMethod = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Orders.Include(o => o.OrderItems).Include(o => o.User).AsQueryable();
            if (!string.IsNullOrEmpty(orderStatus)) query = query.Where(o => o.OrderStatus == orderStatus);
            if (!string.IsNullOrEmpty(paymentMethod)) query = query.Where(o => o.PaymentMethod == paymentMethod);
            var totalCount = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { orders = orders.Select(MapToOrderDto), totalCount, pageNumber, pageSize });
        }

        [HttpGet("admin/statistics")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var inProgressCount = await _context.Orders.CountAsync(o => o.OrderStatus == "In Progress");
            var outForDeliveryCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Out For Delivery");
            var deliveredCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Delivered");
            var codCount = await _context.Orders.CountAsync(o => o.PaymentMethod == "Cash On Delivery");
            var codRevenue = await _context.Orders.Where(o => o.PaymentMethod == "Cash On Delivery").SumAsync(o => o.TotalAmount);
            return Ok(new
            {
                totalOrders,
                totalRevenue,
                orderStatuses = new { inProgress = inProgressCount, outForDelivery = outForDeliveryCount, delivered = deliveredCount },
                paymentMethods = new { codCount, codRevenue }
            });
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return BadRequest("User not found");
            if (!int.TryParse(userIdClaim.Value, out var userId)) return BadRequest("Invalid user ID");
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.UserId != userId) return Forbid();
            if (order.OrderStatus != "In Progress") return BadRequest("Order can only be cancelled when in progress");
            order.OrderStatus = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Order cancelled", order = MapToOrderDto(order) });
        }

        // ================== HELPERS ==================
        private string GenerateOrderNumber()
        {
            var today = DateTime.UtcNow;
            var dateString = today.ToString("yyyyMMdd");
            var dailyOrderCount = _context.Orders.Count(o => o.CreatedAt.Date == today.Date) + 1;
            return $"ORD-{dateString}-{dailyOrderCount:D5}";
        }

        private OrderDto MapToOrderDto(Order order) => new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SubTotal = order.SubTotal,
            DeliveryCharges = order.DeliveryCharges,
            TotalAmount = order.TotalAmount,
            FullName = order.FullName,
            PhoneNumber = order.PhoneNumber,
            Street = order.Street,
            City = order.City,
            PostalCode = order.PostalCode,
            OrderStatus = order.OrderStatus,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                Price = oi.Price,
                Quantity = oi.Quantity,
                TotalPrice = oi.TotalPrice,
                ImagePath = oi.ImagePath,
                CreatedAt = oi.CreatedAt
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            DeliveredAt = order.DeliveredAt
        };
    }
}