using Microsoft.AspNetCore.Mvc;
using Gerente.Models;
using Gerente.Filters;
using Npgsql;

namespace Gerente.Controllers
{
    public class ParametroAmbienteController : BaseController
    {
        public ParametroAmbienteController(IConfiguration configuration) : base(configuration)
        {
        }

        [RequireConfigurationsAccess]
        public IActionResult Index()
        {
            var ambientes = ObterAmbientes();
            return View(ambientes);
        }

        [RequireConfigurationsAccess]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireConfigurationsAccess]
        public IActionResult Create(ParametroAmbiente model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    InserirAmbiente(model);
                    TempData["SuccessMessage"] = "Ambiente criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao criar ambiente: {ex.Message}";
                }
            }

            return View(model);
        }

        [RequireConfigurationsAccess]
        public IActionResult Edit(int id)
        {
            var ambiente = ObterAmbientePorId(id);
            if (ambiente == null)
            {
                TempData["ErrorMessage"] = "Ambiente não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            return View(ambiente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireConfigurationsAccess]
        public IActionResult Edit(ParametroAmbiente model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    AtualizarAmbiente(model);
                    TempData["SuccessMessage"] = "Ambiente atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar ambiente: {ex.Message}";
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
                ExcluirAmbiente(id);
                return Json(new { success = true, message = "Ambiente excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao excluir ambiente: {ex.Message}" });
            }
        }

        private List<ParametroAmbiente> ObterAmbientes()
        {
            var ambientes = new List<ParametroAmbiente>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return ambientes;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nome, ativo, data_criacao FROM parametros_ambientes ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ambientes.Add(new ParametroAmbiente
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

            return ambientes;
        }

        private ParametroAmbiente? ObterAmbientePorId(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return null;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nome, ativo, data_criacao FROM parametros_ambientes WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ParametroAmbiente
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

        private void InserirAmbiente(ParametroAmbiente model)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("INSERT INTO parametros_ambientes (nome, ativo, data_criacao) VALUES (@nome, @ativo, CURRENT_TIMESTAMP)", conn))
                {
                    cmd.Parameters.AddWithValue("@nome", model.Nome);
                    cmd.Parameters.AddWithValue("@ativo", model.Ativo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AtualizarAmbiente(ParametroAmbiente model)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("UPDATE parametros_ambientes SET nome = @nome, ativo = @ativo WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@nome", model.Nome);
                    cmd.Parameters.AddWithValue("@ativo", model.Ativo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ExcluirAmbiente(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM parametros_ambientes WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
} 