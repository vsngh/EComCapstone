using ECommerce.Application.Carts.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Carts;

public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CartResponse?> GetCartAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(userId, cancellationToken);

        return cart is null ? null : MapToResponse(cart);
    }

    public async Task<CartResponse> AddItemAsync(
        Guid userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);

        var cart = await _cartRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        var isNewCart = cart is null;

        if (cart is null)
        {
            cart = new Cart(userId);
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(request.ProductId);

        cart.AddItem(product, request.Quantity);

        if (isNewCart)
        {
            await _cartRepository.AddAsync(cart, cancellationToken);
        }
        else
        {
            _cartRepository.Update(cart);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Item {ProductId} x{Quantity} added to cart for user {UserId}",
            request.ProductId,
            request.Quantity,
            userId);

        return MapToResponse(cart);
    }

    public async Task<CartResponse> UpdateItemQuantityAsync(
        Guid userId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var cart = await GetCartAsyncOrThrow(userId, cancellationToken);

        cart.UpdateItemQuantity(productId, quantity);

        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cart item {ProductId} updated to x{Quantity} for user {UserId}",
            productId,
            quantity,
            userId);

        return MapToResponse(cart);
    }

    public async Task<CartResponse> RemoveItemAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var cart = await GetCartAsyncOrThrow(userId, cancellationToken);

        cart.RemoveItem(productId);

        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cart item {ProductId} removed for user {UserId}",
            productId,
            userId);

        return MapToResponse(cart);
    }

    public async Task ClearAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await GetCartAsyncOrThrow(userId, cancellationToken);

        cart.Clear();

        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cart cleared for user {UserId}", userId);
    }

    private async Task<Cart> GetCartAsyncOrThrow(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _cartRepository.GetActiveByUserIdAsync(userId, cancellationToken)
            ?? throw new CartNotFoundException(userId);
    }

    private async Task EnsureUserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            throw new UserNotFoundException(userId);
        }
    }

    private static CartResponse MapToResponse(Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemResponse(
                i.ProductId,
                i.Product?.Name ?? "Unknown product",
                i.Product?.Sku ?? string.Empty,
                i.UnitPrice.Amount,
                i.Quantity,
                i.GetLineTotal().Amount))
            .ToList();

        return new CartResponse(
            cart.Id,
            cart.UserId,
            cart.TotalItems,
            items.Sum(i => i.LineTotal),
            items);
    }
}