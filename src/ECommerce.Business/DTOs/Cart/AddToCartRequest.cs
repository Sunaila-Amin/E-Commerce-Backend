namespace ECommerce.Business.DTOs.Cart;

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
