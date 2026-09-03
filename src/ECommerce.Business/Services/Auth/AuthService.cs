using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Auth;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;

namespace ECommerce.Business.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _tokenService;

    public AuthService(IUnitOfWork uow, IJwtTokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var exists = await _uow.Users.AnyAsync(
            u => u.Email == request.Email,
            cancellationToken);

        if (exists)
        {
            return ServiceResult<AuthResponse>.Failure("A user with this email already exists.");
        }

        var userRole = await _uow.Roles.GetByNameAsync(RoleName.User, cancellationToken);
        if (userRole is null)
        {
            return ServiceResult<AuthResponse>.Failure("Role configuration is missing.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedBy = request.Email,
            Roles = new List<Role> { userRole }
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var response = BuildAuthResponse(user);
        return ServiceResult<AuthResponse>.Success(response, "Registration successful.");
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthResponse>.Failure("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthResponse>.Failure("This account is inactive.");
        }

        var withRoles = await _uow.Users.GetByIdWithRolesAsync(user.Id, cancellationToken);
        var roles = (withRoles?.Roles ?? new List<Role>())
            .Select(r => r.Name.ToString())
            .ToList();

        var response = BuildAuthResponse(user, roles);
        return ServiceResult<AuthResponse>.Success(response, "Login successful.");
    }

    public async Task<ServiceResult<UserDto>> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<UserDto>.Failure("User not found.");
        }

        var dto = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = user.Roles.Select(r => r.Name.ToString()).ToList()
        };

        return ServiceResult<UserDto>.Success(dto);
    }

    private AuthResponse BuildAuthResponse(User user, IReadOnlyList<string>? roles = null)
    {
        roles ??= new List<string> { RoleName.User.ToString() };

        var token = _tokenService.GenerateToken(user, roles);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles,
            Token = token,
            ExpiresAt = _tokenService.ExpiresAt
        };
    }
}
