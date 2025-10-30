using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;
using ShopNongSan.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Cookie Authentication
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
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// DB
builder.Services.AddDbContext<NongSanContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// VNPAY settings + service
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddHttpContextAccessor();

// Policies (tuỳ dùng)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("AdminOrStaff", p => p.RequireRole("Admin", "Staff"));
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer", "Admin"));
});

var app = builder.Build();

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

// "/" -> Trang chủ Customer
app.MapGet("/", () => Results.Redirect("/Customer/Home"));

// Cho các attribute route tuyệt đối bên dưới
app.MapControllers();

app.Run();
