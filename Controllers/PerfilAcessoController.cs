using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;
using System.Collections.Generic;
using Gerente.Filters;

namespace Gerente.Controllers
{
    [RequireConfigurationsAccess]
    public class PerfilAcessoController : Controller
    {
        private readonly IConfiguration _configuration;

        public PerfilAcessoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var perfis = ObterTodosPerfis();
            return View(perfis);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PerfilAcessoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PerfilAcessoViewModel perfilViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se o nome já existe
                    if (NomeExiste(perfilViewModel.Nome))
                    {
                        ModelState.AddModelError("Nome", "Já existe um perfil com este nome.");
                        return View(perfilViewModel);
                    }

                    // Salvar perfil
                    SalvarPerfil(perfilViewModel);

                    TempData["Sucesso"] = "Perfil de acesso cadastrado com sucesso!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["Erro"] = "Erro ao cadastrar perfil. Tente novamente.";
                }
            }

            return View(perfilViewModel);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var perfil = ObterPerfilPorId(id);
            if (perfil == null)
            {
                TempData["Erro"] = "Perfil não encontrado.";
                return RedirectToAction("Index");
            }

            var perfilViewModel = new PerfilAcessoViewModel
            {
                Id = perfil.Id,
                Nome = perfil.Nome,
                Descricao = perfil.Descricao,
                AcessoConfiguracoes = perfil.AcessoConfiguracoes,
                AcessoUsuarios = perfil.AcessoUsuarios,
                AcessoProjetos = perfil.AcessoProjetos,
                AcessoRelatorios = perfil.AcessoRelatorios,
                AcessoTotal = perfil.AcessoTotal,
                Ativo = perfil.Ativo
            };

            return View(perfilViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PerfilAcessoViewModel perfilViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se o nome já existe (exceto para o perfil atual)
                    if (NomeExiste(perfilViewModel.Nome, perfilViewModel.Id))
                    {
                        ModelState.AddModelError("Nome", "Já existe um perfil com este nome.");
                        return View(perfilViewModel);
                    }

                    // Atualizar perfil
                    AtualizarPerfil(perfilViewModel);

                    TempData["Sucesso"] = "Perfil de acesso atualizado com sucesso!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["Erro"] = "Erro ao atualizar perfil. Tente novamente.";
                }
            }

            return View(perfilViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                // Verificar se o perfil existe
                var perfil = ObterPerfilPorId(id);
                if (perfil == null)
                {
                    return Json(new { success = false, message = "Perfil não encontrado." });
                }

                // Verificar se não é um perfil padrão
                if (perfil.Nome == "Administrador" || perfil.Nome == "Usuário")
                {
                    return Json(new { success = false, message = "Não é possível excluir perfis padrão do sistema." });
                }

                // Verificar se há usuários usando este perfil
                if (PerfilEmUso(id))
                {
                    return Json(new { success = false, message = "Não é possível excluir este perfil pois há usuários associados a ele." });
                }

                ExcluirPerfil(id);
                return Json(new { success = true, message = "Perfil excluído com sucesso!" });
            }
            catch (Npgsql.PostgresException ex)
            {
                string mensagemErro;
                switch (ex.SqlState)
                {
                    case "23503": // Foreign key violation
                        mensagemErro = "Não é possível excluir este perfil pois ele está sendo utilizado por usuários.";
                        break;
                    case "23505": // Unique violation
                        mensagemErro = "Erro de duplicação de dados.";
                        break;
                    default:
                        mensagemErro = $"Erro no banco de dados: {ex.Message}";
                        break;
                }
                return Json(new { success = false, message = mensagemErro });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Erro ao excluir perfil. Tente novamente." });
            }
        }

        private List<PerfilAcesso> ObterTodosPerfis()
        {
            var perfis = new List<PerfilAcesso>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_total, ativo, data_criacao, data_atualizacao FROM perfis_acesso ORDER BY nome", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perfis.Add(new PerfilAcesso
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Nome = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Descricao = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                AcessoConfiguracoes = reader.IsDBNull(3) ? false : reader.GetBoolean(3),
                                AcessoUsuarios = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                                AcessoProjetos = reader.IsDBNull(5) ? false : reader.GetBoolean(5),
                                AcessoRelatorios = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                                AcessoTotal = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                                Ativo = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                                DataCriacao = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9),
                                DataAlteracao = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10)
                            });
                        }
                    }
                }
            }

            return perfis;
        }

        private PerfilAcesso? ObterPerfilPorId(int id)
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
                    "SELECT id, nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_total, ativo, data_criacao, data_atualizacao FROM perfis_acesso WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
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
                                Ativo = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                                DataCriacao = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9),
                                DataAlteracao = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10)
                            };
                        }
                    }
                }
            }

            return null;
        }

        private bool NomeExiste(string nome, int? idExcluir = null)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM perfis_acesso WHERE nome = @nome";
                if (idExcluir.HasValue)
                {
                    sql += " AND id != @id";
                }

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nome", nome);
                    if (idExcluir.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@id", idExcluir.Value);
                    }
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private bool PerfilEmUso(int perfilId)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios WHERE perfil_acesso_id = @perfilId", conn))
                {
                    cmd.Parameters.AddWithValue("@perfilId", perfilId);
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void SalvarPerfil(PerfilAcessoViewModel perfilViewModel)
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
                    @"INSERT INTO perfis_acesso (nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_total, ativo) 
                      VALUES (@nome, @descricao, @acessoConfiguracoes, @acessoUsuarios, @acessoProjetos, @acessoRelatorios, @acessoTotal, @ativo)", conn))
                {
                    cmd.Parameters.AddWithValue("@nome", perfilViewModel.Nome);
                    cmd.Parameters.AddWithValue("@descricao", perfilViewModel.Descricao ?? "");
                    cmd.Parameters.AddWithValue("@acessoConfiguracoes", perfilViewModel.AcessoConfiguracoes);
                    cmd.Parameters.AddWithValue("@acessoUsuarios", perfilViewModel.AcessoUsuarios);
                    cmd.Parameters.AddWithValue("@acessoProjetos", perfilViewModel.AcessoProjetos);
                    cmd.Parameters.AddWithValue("@acessoRelatorios", perfilViewModel.AcessoRelatorios);
                    cmd.Parameters.AddWithValue("@acessoTotal", perfilViewModel.AcessoTotal);
                    cmd.Parameters.AddWithValue("@ativo", perfilViewModel.Ativo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AtualizarPerfil(PerfilAcessoViewModel perfilViewModel)
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
                    @"UPDATE perfis_acesso 
                      SET nome = @nome, descricao = @descricao, acesso_configuracoes = @acessoConfiguracoes, 
                          acesso_usuarios = @acessoUsuarios, acesso_projetos = @acessoProjetos, 
                          acesso_relatorios = @acessoRelatorios, acesso_total = @acessoTotal, ativo = @ativo 
                      WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", perfilViewModel.Id);
                    cmd.Parameters.AddWithValue("@nome", perfilViewModel.Nome);
                    cmd.Parameters.AddWithValue("@descricao", perfilViewModel.Descricao ?? "");
                    cmd.Parameters.AddWithValue("@acessoConfiguracoes", perfilViewModel.AcessoConfiguracoes);
                    cmd.Parameters.AddWithValue("@acessoUsuarios", perfilViewModel.AcessoUsuarios);
                    cmd.Parameters.AddWithValue("@acessoProjetos", perfilViewModel.AcessoProjetos);
                    cmd.Parameters.AddWithValue("@acessoRelatorios", perfilViewModel.AcessoRelatorios);
                    cmd.Parameters.AddWithValue("@acessoTotal", perfilViewModel.AcessoTotal);
                    cmd.Parameters.AddWithValue("@ativo", perfilViewModel.Ativo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ExcluirPerfil(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM perfis_acesso WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("Perfil não encontrado ou já foi excluído.");
                    }
                }
            }
        }
    }
} 