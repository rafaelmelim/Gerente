using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Gerente.Controllers;

public class RelatoriosController : BaseController
{
    public RelatoriosController(IConfiguration configuration) : base(configuration)
    {
    }

    public IActionResult Index()
    {
        if (!HttpContext.Session.Keys.Contains("UserId"))
            return RedirectToAction("Index", "Login");
        
        return View();
    }
} 