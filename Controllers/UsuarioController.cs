using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;
using System.Collections.Generic; // Added missing import

namespace Gerente.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var usuarios = ObterTodosUsuarios();
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new UsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UsuarioViewModel usuarioViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se o e-mail já existe
                    if (EmailExiste(usuarioViewModel.Email))
                    {
                        ModelState.AddModelError("Email", "Este e-mail já está cadastrado no sistema.");
                        return View(usuarioViewModel);
                    }

                    // Criar hash da senha
                    var senhaHash = HashPassword(usuarioViewModel.Senha);

                    // Salvar usuário
                    SalvarUsuario(usuarioViewModel, senhaHash);

                    TempData["Sucesso"] = "Usuário cadastrado com sucesso!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Erro"] = "Erro ao cadastrar usuário: " + ex.Message;
                }
            }

            return View(usuarioViewModel);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var usuario = ObterUsuarioPorId(id);
            if (usuario == null)
            {
                TempData["Erro"] = "Usuário não encontrado.";
                return RedirectToAction("Index");
            }

            var usuarioViewModel = new UsuarioViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                DataCriacao = usuario.DataCriacao,
                DataAlteracao = usuario.DataAlteracao
            };

            return View(usuarioViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UsuarioViewModel usuarioViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se o e-mail já existe (exceto para o usuário atual)
                    if (EmailExiste(usuarioViewModel.Email, usuarioViewModel.Id))
                    {
                        ModelState.AddModelError("Email", "Este e-mail já está cadastrado no sistema.");
                        return View(usuarioViewModel);
                    }

                    // Se a senha foi alterada, criar novo hash
                    string? senhaHash = null;
                    if (!string.IsNullOrEmpty(usuarioViewModel.Senha))
                    {
                        senhaHash = HashPassword(usuarioViewModel.Senha);
                    }

                    // Atualizar usuário
                    AtualizarUsuario(usuarioViewModel, senhaHash);

                    TempData["Sucesso"] = "Usuário atualizado com sucesso!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Erro"] = "Erro ao atualizar usuário: " + ex.Message;
                }
            }

            return View(usuarioViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                // Verificar se o usuário existe
                var usuario = ObterUsuarioPorId(id);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuário não encontrado." });
                }

                // Verificar se não é o usuário admin
                if (usuario.Email == "admin@dorowcamp.com")
                {
                    return Json(new { success = false, message = "Não é possível excluir o usuário administrador." });
                }

                ExcluirUsuario(id);
                return Json(new { success = true, message = "Usuário excluído com sucesso!" });
            }
            catch (Npgsql.PostgresException ex)
            {
                // Tratar erros específicos do PostgreSQL
                string mensagemErro;
                switch (ex.SqlState)
                {
                    case "23503": // Foreign key violation
                        mensagemErro = "Não é possível excluir este usuário pois ele está sendo utilizado em outras partes do sistema.";
                        break;
                    case "23505": // Unique violation
                        mensagemErro = "Erro de duplicação de dados.";
                        break;
                    case "23514": // Check violation
                        mensagemErro = "Dados inválidos para exclusão.";
                        break;
                    default:
                        mensagemErro = $"Erro no banco de dados: {ex.Message}";
                        break;
                }
                return Json(new { success = false, message = mensagemErro });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao excluir usuário. Tente novamente." });
            }
        }

        private List<Usuario> ObterTodosUsuarios()
        {
            var usuarios = new List<Usuario>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, nome, email, data_criacao, data_alteracao FROM usuarios ORDER BY nome", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new Usuario
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Nome = reader.GetString(reader.GetOrdinal("nome")),
                                Email = reader.GetString(reader.GetOrdinal("email")),
                                DataCriacao = reader.GetDateTime(reader.GetOrdinal("data_criacao")),
                                DataAlteracao = reader.GetDateTime(reader.GetOrdinal("data_alteracao"))
                            });
                        }
                    }
                }
            }

            return usuarios;
        }

        private Usuario? ObterUsuarioPorId(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, nome, email, data_criacao, data_alteracao FROM usuarios WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Usuario
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Nome = reader.GetString(reader.GetOrdinal("nome")),
                                Email = reader.GetString(reader.GetOrdinal("email")),
                                DataCriacao = reader.GetDateTime(reader.GetOrdinal("data_criacao")),
                                DataAlteracao = reader.GetDateTime(reader.GetOrdinal("data_alteracao"))
                            };
                        }
                    }
                }
            }

            return null;
        }

        private bool EmailExiste(string email, int? idExcluir = null)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM usuarios WHERE email = @email";
                if (idExcluir.HasValue)
                {
                    sql += " AND id != @id";
                }

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    if (idExcluir.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@id", idExcluir.Value);
                    }

                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void SalvarUsuario(UsuarioViewModel usuarioViewModel, string senhaHash)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO usuarios (nome, email, senha, data_criacao, data_alteracao) VALUES (@nome, @email, @senha, @dataCriacao, @dataAlteracao)", conn))
                {
                    cmd.Parameters.AddWithValue("@nome", usuarioViewModel.Nome);
                    cmd.Parameters.AddWithValue("@email", usuarioViewModel.Email);
                    cmd.Parameters.AddWithValue("@senha", senhaHash);
                    cmd.Parameters.AddWithValue("@dataCriacao", DateTime.Now);
                    cmd.Parameters.AddWithValue("@dataAlteracao", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AtualizarUsuario(UsuarioViewModel usuarioViewModel, string? senhaHash)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                
                string sql = "UPDATE usuarios SET nome = @nome, email = @email, data_alteracao = @dataAlteracao";
                if (!string.IsNullOrEmpty(senhaHash))
                {
                    sql += ", senha = @senha";
                }
                sql += " WHERE id = @id";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", usuarioViewModel.Id);
                    cmd.Parameters.AddWithValue("@nome", usuarioViewModel.Nome);
                    cmd.Parameters.AddWithValue("@email", usuarioViewModel.Email);
                    cmd.Parameters.AddWithValue("@dataAlteracao", DateTime.Now);
                    
                    if (!string.IsNullOrEmpty(senhaHash))
                    {
                        cmd.Parameters.AddWithValue("@senha", senhaHash);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ExcluirUsuario(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM usuarios WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("Usuário não encontrado ou já foi excluído.");
                    }
                }
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.Convert.ToBase64String(hashedBytes);
            }
        }
    }
} 