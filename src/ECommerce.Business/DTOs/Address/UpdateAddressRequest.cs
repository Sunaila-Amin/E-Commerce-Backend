namespace ECommerce.Business.DTOs.Address;

public class UpdateAddressRequest
{
    public string FullName { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string? City { get; set; }
    public string? State { get; set; }
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}
