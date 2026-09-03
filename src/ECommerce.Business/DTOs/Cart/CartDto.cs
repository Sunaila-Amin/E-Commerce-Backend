namespace ECommerce.Business.DTOs.Cart;

public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<CartItemDto> Items { get; set; } = new List<CartItemDto>();
}
