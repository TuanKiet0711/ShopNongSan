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
    options.AddPolicy("AdminOrStaff", p => p.RequireRole("Admin", "Staff")); // ? thêm
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

// 1) Route riêng cho Area Admin: CHO Admin & Staff
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}"
).RequireAuthorization("AdminOrStaff"); // ? ??i t? AdminOnly -> AdminOrStaff

// 2) Route chung cho các Area khác (n?u có)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// 3) Route m?c ??nh: ?u tiên Area Customer
app.MapControllerRoute(
    name: "customer_default",
    pattern: "{controller=SanPhams}/{action=Index}/{id?}",
    defaults: new { area = "Customer" }
);

// 4) "/" chuy?n th?ng v? Customer
app.MapGet("/", () => Results.Redirect("/Customer/SanPhams"));

app.Run();
