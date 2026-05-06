using Stripe;
using Stripe.Checkout;

namespace FoodiGOAPI.Services
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _config;

        public StripeService(IConfiguration config)
        {
            _config = config;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSessionAsync(decimal amount, string currency, int orderId, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = (long)(amount * 100), // PKR to paisa
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "FoodiGO Order",
                                Description = $"Order #{orderId}"
                            }
                        },
                        Quantity = 1,
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", orderId.ToString() }
                }
            };
            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session.Url;
        }
    }
}