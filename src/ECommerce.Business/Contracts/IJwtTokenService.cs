using ECommerce.Models.Entities;

namespace ECommerce.Business.Contracts;

public interface IJwtTokenService
{
    string GenerateToken(User user, IReadOnlyList<string> roles);
    DateTime ExpiresAt { get; }
}
