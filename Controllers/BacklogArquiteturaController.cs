using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Gerente.Models;
using Gerente.Filters;
using Npgsql;
using System.Text.Json;

namespace Gerente.Controllers
{
    public class BacklogArquiteturaController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BacklogArquiteturaController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment) 
            : base(configuration)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [RequireBacklogArquiteturaAccess]
        public IActionResult Index()
        {
            var backlogs = ObterBacklogs();
            return View(backlogs);
        }

        [RequireBacklogArquiteturaAccess]
        public IActionResult Create()
        {
            ViewBag.Usuarios = ObterUsuarios();
            ViewBag.Prioridades = new List<SelectListItem>
            {
                new SelectListItem { Value = "Baixa", Text = "Baixa" },
                new SelectListItem { Value = "Média", Text = "Média" },
                new SelectListItem { Value = "Alta", Text = "Alta" }
            };
            ViewBag.Ganhos = ObterGanhos();
            ViewBag.Ambientes = ObterAmbientes();
            ViewBag.Progressos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Não Iniciado", Text = "Não Iniciado" },
                new SelectListItem { Value = "Iniciado", Text = "Iniciado" },
                new SelectListItem { Value = "Concluído", Text = "Concluído" }
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireBacklogArquiteturaAccess]
        public IActionResult Create(BacklogArquiteturaViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var ambientes = model.AmbientesSelecionados != null ? 
                        string.Join(",", model.AmbientesSelecionados) : null;

                    // Inserir o backlog primeiro
                    var backlogId = InserirBacklog(model, ambientes, null);

                    // Processar anexos se houver
                    if (model.Anexos != null)
                    {
                        var userId = HttpContext.Session.GetInt32("UserId");
                        foreach (var anexo in model.Anexos)
                        {
                            if (anexo.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(anexo.FileName);
                                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);
                                
                                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                                
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    anexo.CopyTo(stream);
                                }

                                InserirAnexo(backlogId, fileName, anexo.FileName, filePath, anexo.Length, anexo.ContentType, userId ?? 1);
                            }
                        }
                    }

                    // Adicionar comentário inicial se houver
                    if (!string.IsNullOrWhiteSpace(model.NovoComentario))
                    {
                        var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                        InserirComentario(backlogId, model.NovoComentario, userId);
                    }

                    TempData["SuccessMessage"] = "Backlog criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao criar backlog: {ex.Message}";
                }
            }

            ViewBag.Usuarios = ObterUsuarios();
            ViewBag.Prioridades = new List<SelectListItem>
            {
                new SelectListItem { Value = "Baixa", Text = "Baixa" },
                new SelectListItem { Value = "Média", Text = "Média" },
                new SelectListItem { Value = "Alta", Text = "Alta" }
            };
            ViewBag.Ganhos = ObterGanhos();
            ViewBag.Ambientes = ObterAmbientes();
            ViewBag.Progressos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Não Iniciado", Text = "Não Iniciado" },
                new SelectListItem { Value = "Iniciado", Text = "Iniciado" },
                new SelectListItem { Value = "Concluído", Text = "Concluído" }
            };

            return View(model);
        }

        [RequireBacklogArquiteturaAccess]
        public IActionResult Edit(int id)
        {
            var backlog = ObterBacklogPorId(id);
            if (backlog == null)
            {
                TempData["ErrorMessage"] = "Backlog não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var model = new BacklogArquiteturaViewModel
            {
                Id = backlog.Id,
                DescricaoTarefa = backlog.DescricaoTarefa,
                Prioridade = backlog.Prioridade,
                Ganho = backlog.Ganho,
                UsuarioId = backlog.UsuarioId,
                DataInicio = backlog.DataInicio,
                DataFim = backlog.DataFim,
                Progresso = backlog.Progresso,
                AmbientesSelecionados = !string.IsNullOrEmpty(backlog.Ambientes) ? 
                    backlog.Ambientes.Split(',').ToList() : null,
                Anotacoes = backlog.Anotacoes
            };

            ViewBag.Usuarios = ObterUsuarios();
            ViewBag.Prioridades = new List<SelectListItem>
            {
                new SelectListItem { Value = "Baixa", Text = "Baixa" },
                new SelectListItem { Value = "Média", Text = "Média" },
                new SelectListItem { Value = "Alta", Text = "Alta" }
            };
            ViewBag.Ganhos = ObterGanhos();
            ViewBag.Ambientes = ObterAmbientes();
            ViewBag.Progressos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Não Iniciado", Text = "Não Iniciado" },
                new SelectListItem { Value = "Iniciado", Text = "Iniciado" },
                new SelectListItem { Value = "Concluído", Text = "Concluído" }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireBacklogArquiteturaAccess]
        public IActionResult Edit(BacklogArquiteturaViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var ambientes = model.AmbientesSelecionados != null ? 
                        string.Join(",", model.AmbientesSelecionados) : null;

                    // Atualizar o backlog
                    AtualizarBacklog(model, ambientes, null);

                    // Processar novos anexos se houver
                    if (model.Anexos != null)
                    {
                        var userId = HttpContext.Session.GetInt32("UserId");
                        foreach (var anexo in model.Anexos)
                        {
                            if (anexo.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(anexo.FileName);
                                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);
                                
                                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                                
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    anexo.CopyTo(stream);
                                }

                                InserirAnexo(model.Id, fileName, anexo.FileName, filePath, anexo.Length, anexo.ContentType, userId ?? 1);
                            }
                        }
                    }

                    // Adicionar comentário se houver
                    if (!string.IsNullOrWhiteSpace(model.NovoComentario))
                    {
                        var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                        InserirComentario(model.Id, model.NovoComentario, userId);
                    }

                    TempData["SuccessMessage"] = "Backlog atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Erro ao atualizar backlog: {ex.Message}";
                }
            }

            ViewBag.Usuarios = ObterUsuarios();
            ViewBag.Prioridades = new List<SelectListItem>
            {
                new SelectListItem { Value = "Baixa", Text = "Baixa" },
                new SelectListItem { Value = "Média", Text = "Média" },
                new SelectListItem { Value = "Alta", Text = "Alta" }
            };
            ViewBag.Ganhos = ObterGanhos();
            ViewBag.Ambientes = ObterAmbientes();
            ViewBag.Progressos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Não Iniciado", Text = "Não Iniciado" },
                new SelectListItem { Value = "Iniciado", Text = "Iniciado" },
                new SelectListItem { Value = "Concluído", Text = "Concluído" }
            };

            return View(model);
        }

        [HttpPost]
        [RequireBacklogArquiteturaAccess]
        public IActionResult AdicionarComentario(int backlogId, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                return Json(new { success = false, message = "Comentário não pode estar vazio." });
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                InserirComentario(backlogId, comentario, userId.Value);
                return Json(new { success = true, message = "Comentário adicionado com sucesso." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao adicionar comentário: {ex.Message}" });
            }
        }

        [RequireBacklogArquiteturaAccess]
        public IActionResult Details(int id)
        {
            var backlog = ObterBacklogPorId(id);
            if (backlog == null)
            {
                TempData["ErrorMessage"] = "Backlog não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Obter anexos do backlog
            var anexos = ObterAnexosPorBacklog(id);
            
            // Obter comentários do backlog
            var comentarios = ObterComentariosPorBacklog(id);

            ViewBag.Anexos = anexos;
            ViewBag.Comentarios = comentarios;

            return View(backlog);
        }

        [RequireBacklogArquiteturaAccess]
        public IActionResult ObterComentarios(int backlogId)
        {
            var comentarios = ObterComentariosPorBacklog(backlogId);
            return Json(comentarios);
        }

        [HttpPost]
        [RequireBacklogArquiteturaAccess]
        public IActionResult AdicionarAnexo(int backlogId, IFormFileCollection anexos)
        {
            if (anexos == null || !anexos.Any())
            {
                return Json(new { success = false, message = "Nenhum arquivo foi selecionado." });
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                var anexosAdicionados = new List<object>();
                foreach (var anexo in anexos)
                {
                    if (anexo.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(anexo.FileName);
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            anexo.CopyTo(stream);
                        }

                        var anexoId = InserirAnexo(backlogId, fileName, anexo.FileName, filePath, anexo.Length, anexo.ContentType, userId.Value);
                        
                        anexosAdicionados.Add(new { 
                            id = anexoId, 
                            nomeOriginal = anexo.FileName, 
                            tamanho = anexo.Length,
                            dataUpload = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                        });
                    }
                }

                return Json(new { success = true, message = "Anexos adicionados com sucesso.", anexos = anexosAdicionados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao adicionar anexos: {ex.Message}" });
            }
        }

        [HttpPost]
        [RequireBacklogArquiteturaAccess]
        public IActionResult ExcluirAnexo(int anexoId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                var sucesso = ExcluirAnexo(anexoId, userId.Value);
                if (sucesso)
                {
                    return Json(new { success = true, message = "Anexo excluído com sucesso." });
                }
                else
                {
                    return Json(new { success = false, message = "Anexo não encontrado ou sem permissão para excluir." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro ao excluir anexo: {ex.Message}" });
            }
        }

        [HttpPost]
        [RequireBacklogArquiteturaAccess]
        public IActionResult Delete(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                // Log para debug
                System.Diagnostics.Debug.WriteLine($"Tentando excluir backlog ID: {id}");

                var sucesso = ExcluirBacklog(id);
                if (sucesso)
                {
                    return Json(new { success = true, message = "A tarefa foi excluída com sucesso." });
                }
                else
                {
                    return Json(new { success = false, message = "Tarefa não encontrada ou sem permissão para excluir." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na action Delete: {ex.Message}");
                return Json(new { success = false, message = $"Erro ao excluir tarefa: {ex.Message}" });
            }
        }

        private List<BacklogArquitetura> ObterBacklogs()
        {
            var backlogs = new List<BacklogArquitetura>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return backlogs;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT b.id, b.descricao_tarefa, b.prioridade, b.ganho, b.usuario_id, 
                               u.nome as usuario_nome, b.data_inicio, b.data_fim, b.progresso, 
                               b.ambientes, b.anotacoes, b.anexos, b.data_conclusao, 
                               b.data_criacao, b.data_alteracao
                        FROM backlogs_arquitetura b
                        LEFT JOIN usuarios u ON b.usuario_id = u.id
                        ORDER BY b.data_criacao DESC", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                backlogs.Add(new BacklogArquitetura
                                {
                                    Id = reader.GetInt32(0),
                                    DescricaoTarefa = reader.GetString(1),
                                    Prioridade = reader.GetString(2),
                                    Ganho = reader.GetString(3),
                                    UsuarioId = reader.GetInt32(4),
                                    UsuarioNome = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    DataInicio = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                                    DataFim = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                                    Progresso = reader.GetString(8),
                                    Ambientes = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    Anotacoes = reader.IsDBNull(10) ? null : reader.GetString(10),
                                    Anexos = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    DataConclusao = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                                    DataCriacao = reader.GetDateTime(13),
                                    DataAlteracao = reader.GetDateTime(14)
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

            return backlogs;
        }

        private BacklogArquitetura? ObterBacklogPorId(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return null;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT b.id, b.descricao_tarefa, b.prioridade, b.ganho, b.usuario_id, 
                               u.nome as usuario_nome, b.data_inicio, b.data_fim, b.progresso, 
                               b.ambientes, b.anotacoes, b.anexos, b.data_conclusao, 
                               b.data_criacao, b.data_alteracao
                        FROM backlogs_arquitetura b
                        LEFT JOIN usuarios u ON b.usuario_id = u.id
                        WHERE b.id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BacklogArquitetura
                                {
                                    Id = reader.GetInt32(0),
                                    DescricaoTarefa = reader.GetString(1),
                                    Prioridade = reader.GetString(2),
                                    Ganho = reader.GetString(3),
                                    UsuarioId = reader.GetInt32(4),
                                    UsuarioNome = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    DataInicio = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                                    DataFim = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                                    Progresso = reader.GetString(8),
                                    Ambientes = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    Anotacoes = reader.IsDBNull(10) ? null : reader.GetString(10),
                                    Anexos = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    DataConclusao = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                                    DataCriacao = reader.GetDateTime(13),
                                    DataAlteracao = reader.GetDateTime(14)
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

        private int InserirBacklog(BacklogArquiteturaViewModel model, string? ambientes, string? anexos)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO backlogs_arquitetura 
                    (descricao_tarefa, prioridade, ganho, usuario_id, data_inicio, data_fim, 
                     progresso, ambientes, anotacoes, anexos, data_criacao, data_alteracao)
                    VALUES (@descricao, @prioridade, @ganho, @usuarioId, @dataInicio, @dataFim,
                            @progresso, @ambientes, @anotacoes, @anexos, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("@descricao", model.DescricaoTarefa);
                    cmd.Parameters.AddWithValue("@prioridade", model.Prioridade);
                    cmd.Parameters.AddWithValue("@ganho", model.Ganho);
                    cmd.Parameters.AddWithValue("@usuarioId", model.UsuarioId);
                    cmd.Parameters.AddWithValue("@dataInicio", model.DataInicio ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dataFim", model.DataFim ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@progresso", model.Progresso);
                    cmd.Parameters.AddWithValue("@ambientes", ambientes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@anotacoes", model.Anotacoes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@anexos", anexos ?? (object)DBNull.Value);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void AtualizarBacklog(BacklogArquiteturaViewModel model, string? ambientes, string? anexos)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                
                // Verificar se o progresso mudou para "Concluído"
                var backlogAtual = ObterBacklogPorId(model.Id);
                var dataConclusao = backlogAtual?.Progresso != "Concluído" && model.Progresso == "Concluído" 
                    ? DateTime.Now : backlogAtual?.DataConclusao;

                using (var cmd = new NpgsqlCommand(@"
                    UPDATE backlogs_arquitetura 
                    SET descricao_tarefa = @descricao, prioridade = @prioridade, ganho = @ganho, 
                        usuario_id = @usuarioId, data_inicio = @dataInicio, data_fim = @dataFim,
                        progresso = @progresso, ambientes = @ambientes, anotacoes = @anotacoes, 
                        anexos = @anexos, data_conclusao = @dataConclusao, data_alteracao = CURRENT_TIMESTAMP
                    WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@descricao", model.DescricaoTarefa);
                    cmd.Parameters.AddWithValue("@prioridade", model.Prioridade);
                    cmd.Parameters.AddWithValue("@ganho", model.Ganho);
                    cmd.Parameters.AddWithValue("@usuarioId", model.UsuarioId);
                    cmd.Parameters.AddWithValue("@dataInicio", model.DataInicio ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dataFim", model.DataFim ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@progresso", model.Progresso);
                    cmd.Parameters.AddWithValue("@ambientes", ambientes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@anotacoes", model.Anotacoes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@anexos", anexos ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dataConclusao", dataConclusao ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InserirComentario(int backlogId, string comentario, int usuarioId)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO comentarios_backlog (backlog_id, comentario, usuario_id, data_criacao)
                    VALUES (@backlogId, @comentario, @usuarioId, CURRENT_TIMESTAMP)", conn))
                {
                    cmd.Parameters.AddWithValue("@backlogId", backlogId);
                    cmd.Parameters.AddWithValue("@comentario", comentario);
                    cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private List<ComentarioBacklog> ObterComentariosPorBacklog(int backlogId)
        {
            var comentarios = new List<ComentarioBacklog>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return comentarios;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT c.id, c.backlog_id, c.comentario, c.data_criacao, c.usuario_id, u.nome as usuario_nome
                        FROM comentarios_backlog c
                        LEFT JOIN usuarios u ON c.usuario_id = u.id
                        WHERE c.backlog_id = @backlogId
                        ORDER BY c.data_criacao DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@backlogId", backlogId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                comentarios.Add(new ComentarioBacklog
                                {
                                    Id = reader.GetInt32(0),
                                    BacklogId = reader.GetInt32(1),
                                    Comentario = reader.GetString(2),
                                    DataCriacao = reader.GetDateTime(3),
                                    UsuarioId = reader.GetInt32(4),
                                    UsuarioNome = reader.IsDBNull(5) ? null : reader.GetString(5)
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

            return comentarios;
        }

        private List<SelectListItem> ObterUsuarios()
        {
            var usuarios = new List<SelectListItem>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return usuarios;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, nome FROM usuarios WHERE ativo = true ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                usuarios.Add(new SelectListItem
                                {
                                    Value = reader.GetInt32(0).ToString(),
                                    Text = reader.GetString(1)
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

            return usuarios;
        }

        private List<SelectListItem> ObterGanhos()
        {
            var ganhos = new List<SelectListItem>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return ganhos;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT nome FROM parametros_ganhos WHERE ativo = true ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ganhos.Add(new SelectListItem
                                {
                                    Value = reader.GetString(0),
                                    Text = reader.GetString(0)
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

        private List<SelectListItem> ObterAmbientes()
        {
            var ambientes = new List<SelectListItem>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return ambientes;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT nome FROM parametros_ambientes WHERE ativo = true ORDER BY nome", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ambientes.Add(new SelectListItem
                                {
                                    Value = reader.GetString(0),
                                    Text = reader.GetString(0)
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

        private List<AnexoBacklog> ObterAnexosPorBacklog(int backlogId)
        {
            var anexos = new List<AnexoBacklog>();
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return anexos;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT a.id, a.backlog_id, a.nome_arquivo, a.nome_original, a.caminho_arquivo,
                               a.tamanho_bytes, a.tipo_mime, a.usuario_id, u.nome as usuario_nome,
                               a.data_upload
                        FROM anexos_backlog a
                        LEFT JOIN usuarios u ON a.usuario_id = u.id
                        WHERE a.backlog_id = @backlogId
                        ORDER BY a.data_upload DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@backlogId", backlogId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tamanhoBytes = reader.GetInt64(5);
                                var tamanhoFormatado = FormatFileSize(tamanhoBytes);

                                anexos.Add(new AnexoBacklog
                                {
                                    Id = reader.GetInt32(0),
                                    BacklogId = reader.GetInt32(1),
                                    NomeArquivo = reader.GetString(2),
                                    NomeOriginal = reader.GetString(3),
                                    CaminhoArquivo = reader.GetString(4),
                                    TamanhoBytes = tamanhoBytes,
                                    TipoMime = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    UsuarioId = reader.GetInt32(7),
                                    UsuarioNome = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    DataUpload = reader.GetDateTime(9),
                                    TamanhoFormatado = tamanhoFormatado
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

            return anexos;
        }

        private int InserirAnexo(int backlogId, string nomeArquivo, string nomeOriginal, string caminhoArquivo, long tamanhoBytes, string? tipoMime, int usuarioId)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                throw new Exception("String de conexão não configurada.");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO anexos_backlog (backlog_id, nome_arquivo, nome_original, caminho_arquivo, 
                                               tamanho_bytes, tipo_mime, usuario_id, data_upload)
                    VALUES (@backlogId, @nomeArquivo, @nomeOriginal, @caminhoArquivo, 
                            @tamanhoBytes, @tipoMime, @usuarioId, CURRENT_TIMESTAMP)
                    RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("@backlogId", backlogId);
                    cmd.Parameters.AddWithValue("@nomeArquivo", nomeArquivo);
                    cmd.Parameters.AddWithValue("@nomeOriginal", nomeOriginal);
                    cmd.Parameters.AddWithValue("@caminhoArquivo", caminhoArquivo);
                    cmd.Parameters.AddWithValue("@tamanhoBytes", tamanhoBytes);
                    cmd.Parameters.AddWithValue("@tipoMime", tipoMime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private bool ExcluirAnexo(int anexoId, int usuarioId)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return false;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    
                    // Primeiro, obter informações do anexo para excluir o arquivo físico
                    string? caminhoArquivo = null;
                    using (var cmdSelect = new NpgsqlCommand(@"
                        SELECT caminho_arquivo FROM anexos_backlog 
                        WHERE id = @anexoId AND usuario_id = @usuarioId", conn))
                    {
                        cmdSelect.Parameters.AddWithValue("@anexoId", anexoId);
                        cmdSelect.Parameters.AddWithValue("@usuarioId", usuarioId);
                        
                        var result = cmdSelect.ExecuteScalar();
                        if (result != null)
                        {
                            caminhoArquivo = result.ToString();
                        }
                    }

                    if (caminhoArquivo != null)
                    {
                        // Excluir o registro do banco
                        using (var cmdDelete = new NpgsqlCommand(@"
                            DELETE FROM anexos_backlog 
                            WHERE id = @anexoId AND usuario_id = @usuarioId", conn))
                        {
                            cmdDelete.Parameters.AddWithValue("@anexoId", anexoId);
                            cmdDelete.Parameters.AddWithValue("@usuarioId", usuarioId);

                            var rowsAffected = cmdDelete.ExecuteNonQuery();
                            
                            if (rowsAffected > 0)
                            {
                                // Tentar excluir o arquivo físico
                                try
                                {
                                    if (System.IO.File.Exists(caminhoArquivo))
                                    {
                                        System.IO.File.Delete(caminhoArquivo);
                                    }
                                }
                                catch
                                {
                                    // Se não conseguir excluir o arquivo físico, não é crítico
                                }
                                
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log error
            }

            return false;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private bool ExcluirBacklog(int id)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
                return false;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    
                    // Verificar se o backlog existe
                    using (var cmdCheck = new NpgsqlCommand("SELECT id FROM backlogs_arquitetura WHERE id = @id", conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@id", id);
                        var result = cmdCheck.ExecuteScalar();
                        if (result == null)
                        {
                            return false;
                        }
                    }

                    // Excluir anexos físicos primeiro (se existirem)
                    using (var cmdAnexos = new NpgsqlCommand("SELECT caminho_arquivo FROM anexos_backlog WHERE backlog_id = @id", conn))
                    {
                        cmdAnexos.Parameters.AddWithValue("@id", id);
                        using (var reader = cmdAnexos.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var caminhoArquivo = reader.GetString(0);
                                try
                                {
                                    if (System.IO.File.Exists(caminhoArquivo))
                                    {
                                        System.IO.File.Delete(caminhoArquivo);
                                    }
                                }
                                catch
                                {
                                    // Se não conseguir excluir o arquivo físico, continua
                                }
                            }
                        }
                    }

                    // Excluir o backlog (os anexos e comentários serão excluídos automaticamente devido às foreign keys)
                    using (var cmdDelete = new NpgsqlCommand("DELETE FROM backlogs_arquitetura WHERE id = @id", conn))
                    {
                        cmdDelete.Parameters.AddWithValue("@id", id);
                        var rowsAffected = cmdDelete.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine($"Erro ao excluir backlog {id}: {ex.Message}");
                return false;
            }
        }
    }
} 