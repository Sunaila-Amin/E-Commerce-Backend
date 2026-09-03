namespace ECommerce.Business.DTOs.Auth;

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
