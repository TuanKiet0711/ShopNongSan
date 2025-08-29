using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Models;
using System.Diagnostics;

namespace ShopNongSan.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    // [Authorize(Policy = "AdminOnly")]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
