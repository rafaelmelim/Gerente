using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gerente.Models;
using Microsoft.AspNetCore.Http;

namespace Gerente.Controllers;

    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration) : base(configuration)
        {
            _logger = logger;
        }

    public IActionResult Index()
    {
        if (!HttpContext.Session.Keys.Contains("UserId"))
            return RedirectToAction("Index", "Login");
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Login");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
