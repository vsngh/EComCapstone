namespace ECommerce.Api.Configuration;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    public string WebhookSecret { get; set; } = string.Empty;
}