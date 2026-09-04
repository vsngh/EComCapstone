using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IOptions<DatabaseOptions> _options;

    public DatabaseInitializer(
        ECommerceDbContext dbContext,
        IOptions<DatabaseOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.Equals(_options.Value.Provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        SeedData.SeedData.Seed(_dbContext);
    }
}
