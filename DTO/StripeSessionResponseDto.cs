namespace FoodiGOAPI.DTO
{
    public class StripeSessionResponseDto
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public int OrderId { get; set; }
    }
}