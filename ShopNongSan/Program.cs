using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Services;
using Stripe; // 👈 thêm dòng này

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(">>> RUNNING PROGRAM.CS: " + typeof(Program).Assembly.FullName);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<RateLimitService>();
// Cookie Authentication (HTTPS + top-level redirect compatible)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Customer/TaiKhoan/DangNhap";
        options.LogoutPath = "/Customer/TaiKhoan/DangXuat";
        options.AccessDeniedPath = "/Customer/TaiKhoan/KhongCoQuyen";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "ShopNongSan.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // bắt buộc HTTPS
    });

// DB
builder.Services.AddDbContext<NongSanContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// VNPAY settings + service
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddHttpContextAccessor();

// ⭐ STRIPE settings + service
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddScoped<IStripeService, StripeService>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("AdminOrStaff", p => p.RequireRole("Admin", "Staff"));
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer", "Admin"));
});

// Forwarded headers (cho ngrok / proxy)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// ====== THỨ TỰ QUAN TRỌNG ======
app.UseForwardedHeaders(); // ✅ PHẢI ĐỂ TRƯỚC MỌI THỨ KHÁC LIÊN QUAN HTTPS

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route area Admin
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}"
).RequireAuthorization("AdminOrStaff");

// Route cho các Area khác (Customer…)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// "/" -> Trang chủ Customer
app.MapGet("/", () => Results.Redirect("/Customer/Home"));

// Cho các attribute route tuyệt đối (VnPayReturn, IPN, v.v.)
app.MapControllers();

app.Run();
