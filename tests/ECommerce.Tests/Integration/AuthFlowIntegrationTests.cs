using ECommerce.Business.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Integration;

public class AuthFlowIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public AuthFlowIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_Login_Profile_RoundTrips()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";

        var register = await _fixture.Auth.RegisterAsync(new RegisterRequest
        {
            FullName = "Integration User",
            Email = email,
            Password = "Passw0rd!123"
        });

        register.Succeeded.Should().BeTrue();
        register.Data!.Email.Should().Be(email);
        register.Data.Token.Should().NotBeNullOrEmpty();
        register.Data.Roles.Should().Contain("User");

        var login = await _fixture.Auth.LoginAsync(new LoginRequest
        {
            Email = email,
            Password = "Passw0rd!123"
        });

        login.Succeeded.Should().BeTrue();
        login.Data!.UserId.Should().Be(register.Data.UserId);

        var profile = await _fixture.Auth.GetProfileAsync(register.Data.UserId);

        profile.Succeeded.Should().BeTrue();
        profile.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";

        var first = await _fixture.Auth.RegisterAsync(new RegisterRequest
        {
            FullName = "A",
            Email = email,
            Password = "Passw0rd!123"
        });
        var second = await _fixture.Auth.RegisterAsync(new RegisterRequest
        {
            FullName = "B",
            Email = email,
            Password = "Passw0rd!123"
        });

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        second.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var email = $"wp-{Guid.NewGuid():N}@test.com";
        await _fixture.Auth.RegisterAsync(new RegisterRequest
        {
            FullName = "A",
            Email = email,
            Password = "Passw0rd!123"
        });

        var login = await _fixture.Auth.LoginAsync(new LoginRequest
        {
            Email = email,
            Password = "WrongPass!"
        });

        login.Succeeded.Should().BeFalse();
        login.Message.Should().Be("Invalid email or password.");
    }
}
