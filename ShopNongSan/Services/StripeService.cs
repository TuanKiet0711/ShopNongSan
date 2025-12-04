using Microsoft.Extensions.Options;
using ShopNongSan.Models;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopNongSan.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;

        public StripeService(IOptions<StripeSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<string> CreateCheckoutSessionAsync(
            decimal amount,
            string currency,
            string successUrl,
            string cancelUrl)
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
                            UnitAmount = (long)(amount * 100),
                            Currency = currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Thanh toán đơn hàng"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session.Url;
        }
    }
}
