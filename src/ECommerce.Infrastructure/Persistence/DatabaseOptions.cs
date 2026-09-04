namespace ECommerce.Infrastructure.Persistence;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "InMemory";
    public string ConnectionString { get; set; } = string.Empty;
}
