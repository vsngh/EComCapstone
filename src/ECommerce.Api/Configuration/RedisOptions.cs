namespace ECommerce.Api.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
