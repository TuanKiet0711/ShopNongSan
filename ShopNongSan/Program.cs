using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

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
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Policies
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

// 1) Route riêng cho Area Admin
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}"
).RequireAuthorization("AdminOrStaff");

// 2) Route cho các Area khác
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 3) Route m?c ??nh (?u tiên Customer)
app.MapControllerRoute(
    name: "customer_default",
    pattern: "{controller=SanPhams}/{action=Index}/{id?}",
    defaults: new { area = "Customer" });

// 4) "/" -> Trang ch? Customer/Home/Index
app.MapGet("/", () => Results.Redirect("/Customer/Home"));

app.Run();
