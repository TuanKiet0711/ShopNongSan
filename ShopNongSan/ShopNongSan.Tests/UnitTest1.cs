using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Services;

namespace ShopNongSan.Tests;

public class RateLimitServiceTests
{
    private sealed class TestNongSanContext : NongSanContext
    {
        public TestNongSanContext(DbContextOptions<NongSanContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use InMemory provider configured in tests.
        }
    }

    private static RateLimitService CreateService(out TestNongSanContext db)
    {
        var options = new DbContextOptionsBuilder<NongSanContext>()
            .UseInMemoryDatabase($"RateLimitTests_{Guid.NewGuid()}")
            .Options;

        db = new TestNongSanContext(options);
        return new RateLimitService(db);
    }

    [Fact]
    public void BuildKey_NormalizesUsernameAndIp()
    {
        var key = RateLimitService.BuildKey("  UserName ", " 127.0.0.1 ");

        Assert.Equal("username|127.0.0.1", key);
    }

    [Fact]
    public async Task IsBlocked_ReturnsFalse_BeforeThreshold()
    {
        var service = CreateService(out _);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 3, window);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 3, window);

        Assert.False(status.IsBlocked);
        Assert.Equal(1, status.FailCount);
    }

    [Fact]
    public async Task IsBlocked_ReturnsTrue_AtThreshold()
    {
        var service = CreateService(out _);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 3, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 3, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 3, window);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 3, window);

        Assert.True(status.IsBlocked);
        Assert.Equal(3, status.FailCount);
        Assert.NotNull(status.BlockUntil);
    }

    [Fact]
    public async Task Reset_ClearsFailCount()
    {
        var service = CreateService(out _);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 3, window);
        await service.ResetAsync(key, endpoint);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 3, window);

        Assert.False(status.IsBlocked);
        Assert.Equal(0, status.FailCount);
    }

    [Fact]
    public async Task Keys_Are_Isolated_ByUsernameAndIp()
    {
        var service = CreateService(out _);
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        var keyA = RateLimitService.BuildKey("user", "1.1.1.1");
        var keyB = RateLimitService.BuildKey("user", "2.2.2.2");

        await service.RegisterHitAsync(keyA, endpoint, maxCount: 2, window);
        await service.RegisterHitAsync(keyA, endpoint, maxCount: 2, window);

        var statusA = await service.IsBlockedAsync(keyA, endpoint, maxFail: 2, window);
        var statusB = await service.IsBlockedAsync(keyB, endpoint, maxFail: 2, window);

        Assert.True(statusA.IsBlocked);
        Assert.False(statusB.IsBlocked);
        Assert.Equal(0, statusB.FailCount);
    }
}
