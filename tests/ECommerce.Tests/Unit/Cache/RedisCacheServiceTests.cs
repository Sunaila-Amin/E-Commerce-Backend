using System.Text.Json;
using ECommerce.Data.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace ECommerce.Tests.Unit.Cache;

public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _distributed = new();
    private readonly RedisCacheService _sut;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RedisCacheServiceTests()
    {
        _sut = new RedisCacheService(_distributed.Object);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheEmpty_InvokesFactoryAndCaches()
    {
        _distributed.Setup(d => d.GetAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var factoryCalled = false;
        var result = await _sut.GetOrSetAsync("key", async () =>
        {
            factoryCalled = true;
            return new TestDto { Id = 5, Name = "x" };
        });

        var value = (TestDto?)result;
        value.Should().NotBeNull();
        value!.Id.Should().Be(5);
        factoryCalled.Should().BeTrue();
        _distributed.Verify(d => d.SetAsync(
            "key",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCached_ReturnsWithoutInvokingFactory()
    {
        var cachedBytes = JsonSerializer.SerializeToUtf8Bytes(new TestDto { Id = 9, Name = "cached" }, _json);
        _distributed.Setup(d => d.GetAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        var factoryCalled = false;
        var result = await _sut.GetOrSetAsync("key", async () =>
        {
            factoryCalled = true;
            return new TestDto { Id = 1, Name = "fresh" };
        });

        var value = (TestDto?)result;
        value!.Id.Should().Be(9);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WhenKeyMissing_ReturnsNull()
    {
        _distributed.Setup(d => d.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var result = await _sut.GetAsync<TestDto>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToCache()
    {
        await _sut.RemoveAsync("key");

        _distributed.Verify(d => d.RemoveAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
