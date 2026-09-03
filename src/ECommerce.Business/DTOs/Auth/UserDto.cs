namespace ECommerce.Business.DTOs.Auth;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
