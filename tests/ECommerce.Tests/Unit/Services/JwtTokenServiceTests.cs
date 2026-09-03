using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.Business.Contracts;
using ECommerce.Business.Services.Auth;
using ECommerce.Models.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ECommerce.Tests.Unit.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "ThisIsAVeryLongSecretKeyForTestingJwtTokens_0123456789_abcdefgh",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        });

        _sut = new JwtTokenService(options);
    }

    [Fact]
    public void GenerateToken_AssignsExpiresAt()
    {
        var user = new User { Id = 5, Email = "u@test.com", FullName = "User" };

        _sut.GenerateToken(user, new List<string> { "User" });

        (_sut.ExpiresAt - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var user = new User { Id = 5, Email = "u@test.com", FullName = "User" };

        var token = _sut.GenerateToken(user, new List<string> { "User", "Admin" });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "5");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "u@test.com");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "User");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_WhenNoRoles_OmitsRoleClaim()
    {
        var user = new User { Id = 5, Email = "u@test.com", FullName = "User" };

        var token = _sut.GenerateToken(user, new List<string>());

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }
}
