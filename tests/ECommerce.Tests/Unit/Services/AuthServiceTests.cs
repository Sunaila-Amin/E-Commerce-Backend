using System.Linq.Expressions;
using ECommerce.Business.Abstractions;
using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Auth;
using ECommerce.Business.Services.Auth;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtTokenService> _tokenService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_uow.Object, _tokenService.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsFailure()
    {
        _uow.SetupGet(u => u.Users).Returns(Mock.Of<IUserRepository>());
        _uow.Setup(u => u.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Test",
            Email = "test@test.com",
            Password = "Pass123!"
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task RegisterAsync_WhenRoleMissing_ReturnsFailure()
    {
        var users = new Mock<IUserRepository>();
        var roles = new Mock<IRoleRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        _uow.SetupGet(u => u.Roles).Returns(roles.Object);

        users.Setup(u => u.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        roles.Setup(r => r.GetByNameAsync(RoleName.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Test",
            Email = "test@test.com",
            Password = "Pass123!"
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Role configuration is missing.");
    }

    [Fact]
    public async Task RegisterAsync_WhenValid_ReturnsSuccessWithToken()
    {
        var users = new Mock<IUserRepository>();
        var roles = new Mock<IRoleRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        _uow.SetupGet(u => u.Roles).Returns(roles.Object);

        users.Setup(u => u.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        roles.Setup(r => r.GetByNameAsync(RoleName.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 1, Name = RoleName.User });
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<User, CancellationToken>((u, _) => u.Id = 99);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns("jwt-token");
        _tokenService.Setup(t => t.ExpiresAt).Returns(DateTime.UtcNow.AddHours(1));

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Jane",
            Email = "jane@test.com",
            Password = "Pass123!"
        });

        result.Succeeded.Should().BeTrue();
        result.Data!.Email.Should().Be("jane@test.com");
        result.Data.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsInvalid_ReturnsFailure()
    {
        var users = new Mock<IUserRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        users.Setup(u => u.GetByEmailAsync("bad@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "bad@test.com",
            Password = "wrong"
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenInactive_ReturnsFailure()
    {
        var users = new Mock<IUserRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        users.Setup(u => u.GetByEmailAsync("x@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "x@test.com",
                FullName = "X",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                IsActive = false
            });

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "x@test.com",
            Password = "Pass123!"
        });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("This account is inactive.");
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserNotFound_ReturnsFailure()
    {
        var users = new Mock<IUserRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        users.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetProfileAsync(1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserFound_ReturnsRoles()
    {
        var users = new Mock<IUserRepository>();
        _uow.SetupGet(u => u.Users).Returns(users.Object);
        users.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 1,
                FullName = "Jane",
                Email = "jane@test.com",
                IsActive = true,
                Roles = new List<Role> { new() { Name = RoleName.Admin } }
            });

        var result = await _sut.GetProfileAsync(1);

        result.Succeeded.Should().BeTrue();
        result.Data!.Roles.Should().Contain("Admin");
    }
}
