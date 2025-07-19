using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;
using System.Collections.Generic; // Added missing import
using Gerente.Filters;
using Gerente.Services;

namespace Gerente.Controllers
{
    [RequireUsersAccess]
    public class UsuarioController : BaseController
    {
        private readonly UsuarioAtivacaoService _ativacaoService;

        public UsuarioController(IConfiguration configuration) : base(configuration)
        {
            _ativacaoService = new UsuarioAtivacaoService(configuration);
        }

        public IActionResult Index()
        {
            var usuarios = ObterTodosUsuarios();
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new UsuarioViewModel();
            ViewBag.PerfisAcesso = ObterPerfisAcesso();
            return View(viewModel);
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
                        ViewBag.PerfisAcesso = ObterPerfisAcesso();
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

            // Garantir que ViewBag.PerfisAcesso esteja sempre definido
            ViewBag.PerfisAcesso = ObterPerfisAcesso();
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
                PerfilAcessoId = usuario.PerfilAcessoId,
                PerfilAcessoNome = usuario.PerfilAcessoNome,
                DataCriacao = usuario.DataCriacao,
                DataAlteracao = usuario.DataAlteracao,
                Ativo = usuario.Ativo
            };

            ViewBag.PerfisAcesso = ObterPerfisAcesso();
            return View(usuarioViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioViewModel usuarioViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se o e-mail já existe (exceto para o usuário atual)
                    if (EmailExiste(usuarioViewModel.Email, usuarioViewModel.Id))
                    {
                        ModelState.AddModelError("Email", "Este e-mail já está cadastrado no sistema.");
                        ViewBag.PerfisAcesso = ObterPerfisAcesso();
                        return View(usuarioViewModel);
                    }

                    // Obter dados atuais do usuário para verificar se foi ativado
                    var usuarioAtual = ObterUsuarioPorId(usuarioViewModel.Id);
                    bool foiAtivado = usuarioAtual != null && !usuarioAtual.Ativo && usuarioViewModel.Ativo;

                    // Se a senha foi alterada, criar novo hash
                    string? senhaHash = null;
                    if (!string.IsNullOrEmpty(usuarioViewModel.Senha))
                    {
                        senhaHash = HashPassword(usuarioViewModel.Senha);
                    }

                    // Atualizar usuário
                    AtualizarUsuario(usuarioViewModel, senhaHash);

                    // Se o usuário foi ativado, enviar e-mail
                    if (foiAtivado)
                    {
                        try
                        {
                            // Usar a senha do campo "Confirmar Nova Senha" se estiver preenchida
                            string senhaParaEmail = "";
                            if (!string.IsNullOrEmpty(usuarioViewModel.ConfirmarSenha))
                            {
                                senhaParaEmail = usuarioViewModel.ConfirmarSenha;
                                Console.WriteLine($"Usando senha do formulário: {senhaParaEmail}");
                            }
                            else
                            {
                                // Se não houver senha no formulário, gerar uma nova
                                senhaParaEmail = GerarSenhaTemporaria();
                                Console.WriteLine($"Gerando nova senha: {senhaParaEmail}");
                            }

                            // Criptografar a senha antes de enviar no email
                            string senhaCriptografada = CryptoUtils.Encrypt(senhaParaEmail);

                            await _ativacaoService.EnviarEmailAtivacaoAsync(
                                usuarioViewModel.Email, 
                                usuarioViewModel.Nome, 
                                senhaCriptografada
                            );

                            TempData["Sucesso"] = "Usuário atualizado com sucesso! E-mail de ativação enviado com a senha definida.";
                        }
                        catch (Exception ex)
                        {
                            TempData["Sucesso"] = "Usuário atualizado com sucesso!";
                            TempData["Aviso"] = "E-mail de ativação não foi enviado: " + ex.Message;
                        }
                    }
                    else
                    {
                        TempData["Sucesso"] = "Usuário atualizado com sucesso!";
                    }

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Erro"] = "Erro ao atualizar usuário: " + ex.Message;
                }
            }

            // Garantir que ViewBag.PerfisAcesso esteja sempre definido
            ViewBag.PerfisAcesso = ObterPerfisAcesso();
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
            catch (Exception)
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
                    @"SELECT u.id, u.nome, u.email, u.data_criacao, u.data_atualizacao, 
                             u.perfil_acesso_id, p.nome as perfil_nome, u.ativo 
                      FROM usuarios u 
                      LEFT JOIN perfis_acesso p ON u.perfil_acesso_id = p.id 
                      ORDER BY u.nome", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new Usuario
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Nome = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PerfilAcessoId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                                PerfilAcessoNome = reader.IsDBNull(6) ? null : reader.GetString(6),
                                DataCriacao = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                DataAlteracao = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                Ativo = reader.IsDBNull(7) ? true : reader.GetBoolean(7)
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
                    @"SELECT u.id, u.nome, u.email, u.data_criacao, u.data_atualizacao, 
                             u.perfil_acesso_id, p.nome as perfil_nome, u.ativo 
                      FROM usuarios u 
                      LEFT JOIN perfis_acesso p ON u.perfil_acesso_id = p.id 
                      WHERE u.id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Usuario
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Nome = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PerfilAcessoId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                                PerfilAcessoNome = reader.IsDBNull(6) ? null : reader.GetString(6),
                                DataCriacao = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                DataAlteracao = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                Ativo = reader.IsDBNull(7) ? true : reader.GetBoolean(7)
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
                    "INSERT INTO usuarios (nome, email, senha, perfil_acesso_id, ativo, data_criacao, data_atualizacao) VALUES (@nome, @email, @senha, @perfilAcessoId, @ativo, @dataCriacao, @dataAlteracao)", conn))
                {
                    cmd.Parameters.AddWithValue("@nome", usuarioViewModel.Nome);
                    cmd.Parameters.AddWithValue("@email", usuarioViewModel.Email);
                    cmd.Parameters.AddWithValue("@senha", senhaHash);
                    cmd.Parameters.AddWithValue("@perfilAcessoId", usuarioViewModel.PerfilAcessoId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ativo", usuarioViewModel.Ativo);
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
                
                string sql = "UPDATE usuarios SET nome = @nome, email = @email, perfil_acesso_id = @perfilAcessoId, ativo = @ativo, data_atualizacao = @dataAlteracao";
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
                    cmd.Parameters.AddWithValue("@perfilAcessoId", usuarioViewModel.PerfilAcessoId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ativo", usuarioViewModel.Ativo);
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

        private List<PerfilAcesso> ObterPerfisAcesso()
        {
            var perfis = new List<PerfilAcesso>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                // Retornar lista vazia em vez de lançar exceção
                return perfis;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, nome FROM perfis_acesso WHERE ativo = true ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                perfis.Add(new PerfilAcesso
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    Nome = reader.IsDBNull(1) ? "" : reader.GetString(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Em caso de erro, retornar lista vazia em vez de propagar a exceção
                return new List<PerfilAcesso>();
            }

            return perfis;
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

        private string GerarSenhaTemporaria()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var senha = new char[8];
            
            for (int i = 0; i < 8; i++)
            {
                senha[i] = caracteres[random.Next(caracteres.Length)];
            }
            
            return new string(senha);
        }

        private void AtualizarSenhaUsuario(int id, string senhaHash)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("UPDATE usuarios SET senha = @senha, data_atualizacao = @dataAlteracao WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@senha", senhaHash);
                    cmd.Parameters.AddWithValue("@dataAlteracao", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
} 