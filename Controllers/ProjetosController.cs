using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Gerente.Controllers;

public class ProjetosController : BaseController
{
    public ProjetosController(IConfiguration configuration) : base(configuration)
    {
    }

    public IActionResult Index()
    {
        if (!HttpContext.Session.Keys.Contains("UserId"))
            return RedirectToAction("Index", "Login");
        
        return View();
    }
} 