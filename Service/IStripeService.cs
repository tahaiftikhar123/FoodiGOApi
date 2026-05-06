namespace FoodiGOAPI.Services
{
    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(decimal amount, string currency, int orderId, string successUrl, string cancelUrl);
    }
}