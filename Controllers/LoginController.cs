using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using Gerente.Services;
using Gerente.Models;

namespace Gerente.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly PasswordResetService _passwordResetService;

        public LoginController(IConfiguration configuration, PasswordResetService passwordResetService)
        {
            _configuration = configuration;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string email, string password, bool lembrarSenha = false)
        {
            // Verificar se os parâmetros não são null
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "E-mail e senha são obrigatórios.";
                return View();
            }

            // Verificar se é uma requisição AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                         Request.Headers["Content-Type"] == "application/json";

            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                
                // Buscar o usuário pelo e-mail
                using (var cmd = new NpgsqlCommand("SELECT id, email, nome, senha FROM usuarios WHERE email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = reader.GetInt32("id");
                            var userEmail = reader.GetString("email");
                            var userName = reader.IsDBNull("nome") ? "" : reader.GetString("nome");
                            var storedPassword = reader.GetString("senha");
                            
                            // Verificar se a senha está em hash ou texto plano (para compatibilidade)
                            var hashedPassword = HashPassword(password);
                            bool passwordValid = false;
                            
                            // Tentar comparar com hash primeiro
                            if (storedPassword == hashedPassword)
                            {
                                passwordValid = true;
                            }
                            // Se não funcionar, tentar com texto plano (para usuários antigos)
                            else if (storedPassword == password)
                            {
                                passwordValid = true;
                                // Atualizar para hash na próxima vez
                                UpdatePasswordToHash(conn, userId, hashedPassword);
                            }
                            
                            if (passwordValid)
                            {
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("Username", userEmail);
                                HttpContext.Session.SetString("UserName", userName);
                                
                                if (isAjax)
                                {
                                    return Json(new { success = true, message = "Login realizado com sucesso!" });
                                }
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "E-mail ou senha inválidos." });
                }
                
                ViewBag.Error = "E-mail ou senha inválidos.";
                return View();
            }
        }

        private void UpdatePasswordToHash(NpgsqlConnection conn, int userId, string hashedPassword)
        {
            try
            {
                using (var cmd = new NpgsqlCommand("UPDATE usuarios SET senha = @senha WHERE id = @userId", conn))
                {
                    cmd.Parameters.AddWithValue("@senha", hashedPassword);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Silenciosamente falhar se não conseguir atualizar
            }
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.Convert.ToBase64String(hashedBytes);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return Json(new { success = false, message = "Por favor, informe um e-mail válido." });
            }

            try
            {
                // Verificar se o e-mail existe no cadastro de usuários
                if (!EmailExisteNoCadastro(request.Email))
                {
                    return Json(new { success = false, message = "E-mail não encontrado no cadastro de usuários do sistema." });
                }

                var result = await _passwordResetService.RequestPasswordResetAsync(request.Email);
                if (result)
                {
                    return Json(new { success = true, message = "E-mail de redefinição enviado com sucesso! Verifique sua caixa de entrada." });
                }
                else
                {
                    return Json(new { success = false, message = "Erro ao enviar e-mail de redefinição. Tente novamente." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erro ao processar solicitação. Tente novamente." });
            }
        }

        private bool EmailExisteNoCadastro(string email)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios WHERE email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Error = "Token inválido.";
                return View("Index");
            }

            var isValid = await _passwordResetService.ValidateTokenAsync(token);
            if (!isValid)
            {
                ViewBag.Error = "Token inválido ou expirado.";
                return View("Index");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpGet]
        public IActionResult TestForgotPassword()
        {
            return Json(new { 
                success = true, 
                message = "Endpoint de teste funcionando!",
                timestamp = DateTime.Now,
                serviceRegistered = _passwordResetService != null
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetConfirm request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
            {
                return Json(new { success = false, message = "Dados inválidos." });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Json(new { success = false, message = "As senhas não coincidem." });
            }

            if (request.NewPassword.Length < 6)
            {
                return Json(new { success = false, message = "A senha deve ter pelo menos 6 caracteres." });
            }

            try
            {
                var result = await _passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
                if (result)
                {
                    return Json(new { success = true, message = "Senha alterada com sucesso! Você pode fazer login com sua nova senha." });
                }
                else
                {
                    return Json(new { success = false, message = "Token inválido ou expirado." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erro ao alterar senha. Tente novamente." });
            }
        }
    }
} 