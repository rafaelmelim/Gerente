using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using Gerente.Services;
using Gerente.Models;

namespace Gerente.Controllers
{
    public class LoginController : BaseController
    {
        private readonly PasswordResetService _passwordResetService;

        public LoginController(IConfiguration configuration, PasswordResetService passwordResetService) : base(configuration)
        {
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
                            var userId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            var userEmail = reader.GetString(1);
                            var userName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            var storedPassword = reader.GetString(3);
                            
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
                                if (userId > 0)
                                {
                                    UpdatePasswordToHash(conn, userId, hashedPassword);
                                }
                            }
                            
                            if (passwordValid)
                            {
                                // Obter informações do perfil de acesso
                                PerfilAcesso? perfilAcesso = null;
                                if (userId > 0)
                                {
                                    perfilAcesso = ObterPerfilAcessoUsuario(userId);
                                }
                                
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("Username", userEmail);
                                HttpContext.Session.SetString("UserName", userName);
                                HttpContext.Session.SetString("UserProfile", perfilAcesso?.Nome ?? "Sem perfil");
                                
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
            if (userId <= 0 || string.IsNullOrEmpty(hashedPassword))
            {
                return;
            }
            
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
            
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    return System.Convert.ToBase64String(hashedBytes);
                }
            }
            catch
            {
                return string.Empty;
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
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }
            
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

        private PerfilAcesso? ObterPerfilAcessoUsuario(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }
            
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                return null;
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    @"SELECT pa.id, pa.nome, pa.descricao, pa.acesso_configuracoes, 
                             pa.acesso_usuarios, pa.acesso_projetos, pa.acesso_relatorios, 
                             pa.acesso_total, pa.ativo
                      FROM usuarios u 
                      LEFT JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id 
                      WHERE u.id = @userId", conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PerfilAcesso
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Nome = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Descricao = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                AcessoConfiguracoes = reader.IsDBNull(3) ? false : reader.GetBoolean(3),
                                AcessoUsuarios = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                                AcessoProjetos = reader.IsDBNull(5) ? false : reader.GetBoolean(5),
                                AcessoRelatorios = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                                AcessoTotal = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                                Ativo = reader.IsDBNull(8) ? false : reader.GetBoolean(8)
                            };
                        }
                    }
                }
            }

            return null;
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