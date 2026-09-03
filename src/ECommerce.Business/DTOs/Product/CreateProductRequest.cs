namespace ECommerce.Business.DTOs.Product;

public class CreateProductRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Sku { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int InitialStock { get; set; }
}
