using Microsoft.AspNetCore.Mvc;
using Gerente.Models;
using Gerente.Filters;
using Npgsql;

namespace Gerente.Controllers
{
    public class ParametroGanhoController : BaseController
    {
        public ParametroGanhoController(IConfiguration configuration) : base(configuration)
        {
        }

        [RequireConfigurationsAccess]
        public IActionResult Index()
        {
            var ganhos = ObterGanhos();
            return View(ganhos);
        }

        [RequireConfigurationsAccess]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireConfigurationsAccess]
        public IActionResult Create(ParametroGanho model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    InserirGanho(model);
                    TempData["SuccessMessage"] = "Ganho criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao criar ganho: {ex.Message}";
                }
            }

            return View(model);
        }

        [RequireConfigurationsAccess]
        public IActionResult Edit(int id)
        {
            var ganho = ObterGanhoPorId(id);
            if (ganho == null)
            {
                TempData["ErrorMessage"] = "Ganho não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            return View(ganho);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireConfigurationsAccess]
        public IActionResult Edit(ParametroGanho model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    AtualizarGanho(model);
                    TempData["SuccessMessage"] = "Ganho atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar ganho: {ex.Message}";
                }
            }

            return View(model);
        }

        [HttpPost]
        [RequireConfigurationsAccess]
        public IActionResult Delete(int id)
        {
            try
            {
                ExcluirGanho(id);
                return Json(new { success = true, message = "Ganho excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao excluir ganho: {ex.Message}" });
            }
        }

        private List<ParametroGanho> ObterGanhos()
        {
            var ganhos = new List<ParametroGanho>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return ganhos;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nome, ativo, data_criacao FROM parametros_ganhos ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ganhos.Add(new ParametroGanho
                                {
                                    Id = reader.GetInt32(0),
                                    Nome = reader.GetString(1),
                                    Ativo = reader.GetBoolean(2),
                                    DataCriacao = reader.GetDateTime(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log error
            }

            return ganhos;
        }

        private ParametroGanho? ObterGanhoPorId(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return null;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nome, ativo, data_criacao FROM parametros_ganhos WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ParametroGanho
                                {
                                    Id = reader.GetInt32(0),
                                    Nome = reader.GetString(1),
                                    Ativo = reader.GetBoolean(2),
                                    DataCriacao = reader.GetDateTime(3)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log error
            }

            return null;
        }

        private void InserirGanho(ParametroGanho model)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("INSERT INTO parametros_ganhos (nome, ativo, data_criacao) VALUES (@nome, @ativo, CURRENT_TIMESTAMP)", conn))
                {
                    cmd.Parameters.AddWithValue("@nome", model.Nome);
                    cmd.Parameters.AddWithValue("@ativo", model.Ativo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AtualizarGanho(ParametroGanho model)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("UPDATE parametros_ganhos SET nome = @nome, ativo = @ativo WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@nome", model.Nome);
                    cmd.Parameters.AddWithValue("@ativo", model.Ativo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ExcluirGanho(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM parametros_ganhos WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
} 