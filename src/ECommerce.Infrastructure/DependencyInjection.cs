using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Cache;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.BackgroundJobs;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Observability;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Outbox;
using ECommerce.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICacheService, InMemoryCacheService>();

        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddDbContext<ECommerceDbContext>(options =>
        {
            var isSqlServer = string.Equals(
                databaseOptions.Provider,
                "SqlServer",
                StringComparison.OrdinalIgnoreCase);

            if (isSqlServer)
            {
                options.UseSqlServer(databaseOptions.ConnectionString);
            }
            else
            {
                var inMemoryName = string.IsNullOrWhiteSpace(databaseOptions.ConnectionString)
                    ? "ECommerceInMemory"
                    : databaseOptions.ConnectionString;

                options.UseInMemoryDatabase(inMemoryName);
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IOutboxProcessingService, OutboxProcessingService>();
        services.AddSingleton<IRequestMetricsService, InMemoryRequestMetricsService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
