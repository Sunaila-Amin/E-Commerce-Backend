using ECommerce.Business.DTOs.Auth;

namespace ECommerce.Business.Contracts;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
}
