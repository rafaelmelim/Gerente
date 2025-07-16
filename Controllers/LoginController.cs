using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Gerente.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string senha, bool lembrarSenha = false)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id FROM usuarios WHERE username = @username AND senha = @senha", conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@senha", senha);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        HttpContext.Session.SetInt32("UserId", Convert.ToInt32(result));
                        HttpContext.Session.SetString("Username", username);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Usuário ou senha inválidos.";
                        return View();
                    }
                }
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
} 