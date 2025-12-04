namespace ShopNongSan.Models   // PHẢI TRÙNG với namespace trong Program.cs
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
