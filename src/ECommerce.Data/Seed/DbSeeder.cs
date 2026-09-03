using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedCategoriesAsync(context);
        await SeedAdminAsync(context);
        await SeedProductsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        context.Roles.AddRange(
            new Role { Name = RoleName.User },
            new Role { Name = RoleName.Admin });

        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var electronics = new Category
        {
            Name = "Electronics",
            Slug = "electronics",
            Description = "Gadgets and electronics"
        };

        var apparel = new Category
        {
            Name = "Apparel",
            Slug = "apparel",
            Description = "Clothing and accessories"
        };

        context.Categories.AddRange(electronics, apparel);

        context.Categories.Add(new Category
        {
            Name = "Smartphones",
            Slug = "smartphones",
            Description = "Mobile phones",
            Parent = electronics
        });

        context.Categories.Add(new Category
        {
            Name = "Laptops",
            Slug = "laptops",
            Description = "Notebooks and laptops",
            Parent = electronics
        });

        context.Categories.Add(new Category
        {
            Name = "T-Shirts",
            Slug = "t-shirts",
            Description = "Casual t-shirts",
            Parent = apparel
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Email == "admin@ecommerce.com"))
        {
            return;
        }

        var adminRole = await context.Roles
            .SingleAsync(r => r.Name == RoleName.Admin);

        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@ecommerce.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsActive = true,
            CreatedBy = "Seeder",
            Roles = new List<Role> { adminRole }
        };

        context.Users.Add(admin);

        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        var smartphones = await context.Categories
            .SingleAsync(c => c.Slug == "smartphones");

        var laptops = await context.Categories
            .SingleAsync(c => c.Slug == "laptops");

        var tshirts = await context.Categories
            .SingleAsync(c => c.Slug == "t-shirts");

        var phone = new Product
        {
            Name = "Smartphone X",
            Slug = "smartphone-x",
            Description = "A high-end smartphone",
            Sku = "P-1001",
            Price = 999.99m,
            Category = smartphones,
            IsActive = true,
            CreatedBy = "Seeder",
            Inventory = new Inventory { Quantity = 50, Reserved = 0, LowStockThreshold = 5 }
        };

        var laptop = new Product
        {
            Name = "Laptop Pro",
            Slug = "laptop-pro",
            Description = "A powerful laptop",
            Sku = "P-1002",
            Price = 1499.99m,
            Category = laptops,
            IsActive = true,
            CreatedBy = "Seeder",
            Inventory = new Inventory { Quantity = 30, Reserved = 0, LowStockThreshold = 3 }
        };

        var tshirt = new Product
        {
            Name = "Cotton T-Shirt",
            Slug = "cotton-t-shirt",
            Description = "Comfortable cotton t-shirt",
            Sku = "P-1003",
            Price = 19.99m,
            Category = tshirts,
            IsActive = true,
            CreatedBy = "Seeder",
            Inventory = new Inventory { Quantity = 200, Reserved = 0, LowStockThreshold = 20 }
        };

        context.Products.AddRange(phone, laptop, tshirt);

        await context.SaveChangesAsync();
    }
}
