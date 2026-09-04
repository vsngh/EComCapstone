using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.SeedData;

public static class SeedData
{
    public static void Seed(ECommerceDbContext dbContext)
    {
        dbContext.Database.EnsureCreated();

        if (dbContext.Users.Any())
        {
            return;
        }

        var admin = User.CreateAdmin(
            "Admin",
            "User",
            new Email("admin@ecommerce.com"),
            BCrypt.Net.BCrypt.HashPassword("AdminPass123!"));

        var customer = User.CreateCustomer(
            "John",
            "Doe",
            new Email("customer@ecommerce.com"),
            BCrypt.Net.BCrypt.HashPassword("CustomerPass123!"));

        dbContext.Users.AddRange(admin, customer);

        var electronics = new Category("Electronics", "Electronic devices and gadgets");
        var fashion = new Category("Fashion", "Clothing and accessories");
        var books = new Category("Books", "Books and publications");

        dbContext.Categories.AddRange(electronics, fashion, books);

        var products = new List<Product>
        {
            Product.Create("Smartphone", "SKU-ELEC-001", new Money(15000m), electronics.Id, "A modern smartphone"),
            Product.Create("Laptop", "SKU-ELEC-002", new Money(60000m), electronics.Id, "High performance laptop"),
            Product.Create("Headphones", "SKU-ELEC-003", new Money(2500m), electronics.Id, "Wireless headphones"),
            Product.Create("T-Shirt", "SKU-FASH-001", new Money(800m), fashion.Id, "Cotton t-shirt"),
            Product.Create("Jeans", "SKU-FASH-002", new Money(2000m), fashion.Id, "Denim jeans"),
            Product.Create("Novel", "SKU-BOOK-001", new Money(500m), books.Id, "A bestselling novel")
        };

        dbContext.Products.AddRange(products);

        var inventory = products.Select(p => new Inventory(p.Id, 100)).ToList();

        dbContext.Inventory.AddRange(inventory);

        dbContext.SaveChanges();
    }
}
