using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace FoodiGOAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private const string StripeSecretKey = "sk_test_YOUR_SECRET_KEY"; // Replace with your key

        public PaymentsController()
        {
            StripeConfiguration.ApiKey = StripeSecretKey;
        }

        [HttpPost("create-payment-intent")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            try
            {
                if (request.Amount <= 0)
                    return BadRequest(new { error = "Invalid amount" });

                // Create payment intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = request.Amount,
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return Ok(new
                {
                    clientSecret = paymentIntent.ClientSecret,
                    publishableKey = "pk_test_YOUR_PUBLISHABLE_KEY"
                });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Error: {ex.Message}" });
            }
        }
    }

    public class PaymentIntentRequest
    {
        public int Amount { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}