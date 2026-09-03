namespace ECommerce.Business.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<CategoryDto> Children { get; set; } = new List<CategoryDto>();
}
