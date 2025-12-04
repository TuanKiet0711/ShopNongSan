using System.Threading.Tasks;

namespace ShopNongSan.Services
{
    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(
            decimal amount,
            string currency,
            string successUrl,
            string cancelUrl);
    }
}
