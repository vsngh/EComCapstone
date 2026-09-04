using ECommerce.Application.Auth;
using ECommerce.Application.Carts;
using ECommerce.Application.Inventory;
using ECommerce.Application.Messaging;
using ECommerce.Application.Notifications;
using ECommerce.Application.Orders;
using ECommerce.Application.Payments;
using ECommerce.Application.Products;
using ECommerce.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<AuthService>();
        services.AddScoped<IUserContext, UserContext>();

        services.AddScoped<ProductService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<CartService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<IPaymentProvider, MockPaymentProvider>();

        services.AddScoped<IEventHandler, OrderPlacedEventHandler>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        return services;
    }
}