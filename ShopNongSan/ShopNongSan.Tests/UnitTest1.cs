using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopNongSan.Models;
using ShopNongSan.Services;

namespace ShopNongSan.Tests;

public class RateLimitServiceTests
{
    private const string LoginEndpoint = "/tai-khoan/dang-nhap";
    private const string LoginUserEndpoint = "/tai-khoan/dang-nhap:user";
    private const string LoginLockoutEndpoint = "/tai-khoan/dang-nhap:lockout";

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
        var rateOptions = Options.Create(new RateLimitOptions
        {
            ResetOnWindowExpiry = true
        });
        return new RateLimitService(db, rateOptions);
    }

    [Fact]
    public async Task IsBlocked_ReturnsFalse_BeforeThreshold()
    {
        var service = CreateService(out _);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 5, window);

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

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 5, window);

        Assert.True(status.IsBlocked);
        Assert.Equal(5, status.FailCount);
        Assert.NotNull(status.BlockUntil);
    }

    [Fact]
    public async Task RegisterHit_DoesNotIncrease_WhileBlocked()
    {
        var service = CreateService(out _);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var blocked = await service.IsBlockedAsync(key, endpoint, maxFail: 5, window);
        Assert.True(blocked.IsBlocked);
        Assert.Equal(5, blocked.FailCount);

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var after = await service.IsBlockedAsync(key, endpoint, maxFail: 5, window);
        Assert.Equal(5, after.FailCount);
    }

    [Fact]
    public async Task Lockout_EndTime_NotBlocked_AtBoundary()
    {
        var service = CreateService(out var db);
        var userKey = RateLimitService.BuildUserKey("user");
        var ipKey = RateLimitService.BuildKey("user", "1.1.1.1");

        for (var j = 0; j < 5; j++)
        {
            await service.RegisterLoginFailAsync(userKey, ipKey, LoginEndpoint, LoginUserEndpoint, LoginLockoutEndpoint);
        }

        var entry = db.DemRateLimits.First(x => x.GiaTriKhoa == userKey && x.Endpoint == LoginLockoutEndpoint);
        entry.KetThucCuaSo = DateTime.UtcNow;
        entry.CapNhatLuc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var lockout = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
        Assert.False(lockout.IsBlocked);
    }

    [Fact]
    public async Task Lockout_Reset_On_Login_Success()
    {
        var service = CreateService(out var db);
        var userKey = RateLimitService.BuildUserKey("user");
        var ipKey = RateLimitService.BuildKey("user", "1.1.1.1");

        for (var j = 0; j < 5; j++)
        {
            await service.RegisterLoginFailAsync(userKey, ipKey, LoginEndpoint, LoginUserEndpoint, LoginLockoutEndpoint);
        }

        var lockout = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
        Assert.True(lockout.IsBlocked);

        await service.ResetLoginAsync(userKey, ipKey, LoginEndpoint, LoginUserEndpoint, LoginLockoutEndpoint);

        var after = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
        Assert.False(after.IsBlocked);

        var userCount = await service.GetPersistentCountAsync(userKey, LoginUserEndpoint);
        Assert.Equal(0, userCount);

        Assert.All(db.DemRateLimits, x => Assert.Equal(0, x.SoLuong));
    }

    [Fact]
    public async Task Lockout_Applies_Across_Ip_For_Same_User()
    {
        var service = CreateService(out _);
        var userKey = RateLimitService.BuildUserKey("user");
        var ipKeyA = RateLimitService.BuildKey("user", "1.1.1.1");
        var ipKeyB = RateLimitService.BuildKey("user", "2.2.2.2");

        for (var j = 0; j < 5; j++)
        {
            await service.RegisterLoginFailAsync(userKey, ipKeyA, LoginEndpoint, LoginUserEndpoint, LoginLockoutEndpoint);
        }

        var lockout = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
        Assert.True(lockout.IsBlocked);

        var blockedIpB = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
        Assert.True(blockedIpB.IsBlocked);
    }

    [Fact]
    public async Task Lockout_Level_Increases_And_Caps()
    {
        var service = CreateService(out var db);
        var userKey = RateLimitService.BuildUserKey("user");
        var ipKey = RateLimitService.BuildKey("user", "1.1.1.1");

        for (var i = 1; i <= 6; i++)
        {
            for (var j = 0; j < 5; j++)
            {
                await service.RegisterLoginFailAsync(userKey, ipKey, LoginEndpoint, LoginUserEndpoint, LoginLockoutEndpoint);
            }

            var lockout = await service.IsLockoutAsync(userKey, LoginLockoutEndpoint);
            Assert.True(lockout.IsBlocked);
            Assert.NotNull(lockout.BlockUntil);
            Assert.Equal(i, lockout.Level);

            var expectedSeconds = Math.Min(60 * (int)Math.Pow(2, i - 1), 3600);
            var remaining = (int)Math.Ceiling((lockout.BlockUntil!.Value - DateTime.UtcNow).TotalSeconds);
            Assert.InRange(remaining, expectedSeconds - 2, expectedSeconds + 2);

            var entry = db.DemRateLimits.First(x => x.GiaTriKhoa == userKey && x.Endpoint == LoginLockoutEndpoint);
            entry.KetThucCuaSo = DateTime.UtcNow.AddSeconds(-1);
            entry.CapNhatLuc = DateTime.UtcNow;

            var userCounter = db.DemRateLimits.First(x => x.GiaTriKhoa == userKey && x.Endpoint == LoginUserEndpoint);
            userCounter.KetThucCuaSo = DateTime.UtcNow.AddSeconds(-1);
            userCounter.CapNhatLuc = DateTime.UtcNow;

            var ipCounter = db.DemRateLimits.First(x => x.GiaTriKhoa == ipKey && x.Endpoint == LoginEndpoint);
            ipCounter.KetThucCuaSo = DateTime.UtcNow.AddSeconds(-1);
            ipCounter.CapNhatLuc = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task WindowExpiry_Resets_FailCount_ToZero()
    {
        var service = CreateService(out var db);
        var key = RateLimitService.BuildKey("user", "1.1.1.1");
        var endpoint = "/tai-khoan/dang-nhap";
        var window = TimeSpan.FromSeconds(60);

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);
        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var entry = db.DemRateLimits.First(x => x.GiaTriKhoa == key && x.Endpoint == endpoint);
        entry.KetThucCuaSo = DateTime.UtcNow.AddSeconds(-1);
        entry.CapNhatLuc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await service.RegisterHitAsync(key, endpoint, maxCount: 5, window);

        var status = await service.IsBlockedAsync(key, endpoint, maxFail: 5, window);
        Assert.Equal(4, status.FailCount);
    }
}
