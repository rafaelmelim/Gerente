using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;

namespace Gerente.Controllers
{
    public class ConfiguracaoEmailController : BaseController
    {
        public ConfiguracaoEmailController(IConfiguration configuration) : base(configuration)
        {
        }

        public IActionResult Index()
        {
            var configuracao = ObterConfiguracaoEmail();
            return View(configuracao);
        }

        [HttpPost]
        public IActionResult Salvar(ConfiguracaoEmail configuracao)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    SalvarConfiguracaoEmail(configuracao);
                    TempData["Sucesso"] = "Configurações de e-mail salvas com sucesso!";
                }
                catch (Exception ex)
                {
                    TempData["Erro"] = "Erro ao salvar configurações: " + ex.Message;
                }
            }
            else
            {
                TempData["Erro"] = "Por favor, verifique os dados informados.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TestarConexao(ConfiguracaoEmail configuracao)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { sucesso = false, mensagem = "Dados inválidos. Corrija os campos obrigatórios." });
            }
            try
            {
                using (var client = new System.Net.Mail.SmtpClient(configuracao.ServidorSmtp, configuracao.Porta))
                {
                    client.EnableSsl = configuracao.SecurityMode == "SSL" || configuracao.SecurityMode == "TLS";
                    client.Credentials = new System.Net.NetworkCredential(configuracao.UsuarioSmtp, configuracao.SenhaSmtp);
                    // TLS é negociado automaticamente se EnableSsl = true
                    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    // Testa conexão enviando comando EHLO
                    client.SendMailAsync(new System.Net.Mail.MailMessage(configuracao.EmailRemetente, configuracao.EmailRemetente, "Teste SMTP", "Teste de conexão SMTP.")).Wait(5000);
                }
                return Json(new { sucesso = true, mensagem = "Conexão SMTP bem-sucedida!" });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao conectar: " + ex.Message });
            }
        }

        private ConfiguracaoEmail ObterConfiguracaoEmail()
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp, security_mode, data_criacao, data_atualizacao FROM configuracoes_email ORDER BY id DESC LIMIT 1", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ConfiguracaoEmail
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                ServidorSmtp = reader.IsDBNull(reader.GetOrdinal("servidor_smtp")) ? "" : reader.GetString(reader.GetOrdinal("servidor_smtp")),
                                Porta = reader.GetInt32(reader.GetOrdinal("porta")),
                                EmailRemetente = reader.IsDBNull(reader.GetOrdinal("email_remetente")) ? "" : reader.GetString(reader.GetOrdinal("email_remetente")),
                                NomeRemetente = reader.IsDBNull(reader.GetOrdinal("nome_remetente")) ? "" : reader.GetString(reader.GetOrdinal("nome_remetente")),
                                UsuarioSmtp = reader.IsDBNull(reader.GetOrdinal("usuario_smtp")) ? "" : reader.GetString(reader.GetOrdinal("usuario_smtp")),
                                SenhaSmtp = reader.IsDBNull(reader.GetOrdinal("senha_smtp")) ? "" : CryptoUtils.Decrypt(reader.GetString(reader.GetOrdinal("senha_smtp"))),
                                SecurityMode = reader.IsDBNull(reader.GetOrdinal("security_mode")) ? "None" : reader.GetString(reader.GetOrdinal("security_mode")),
                                DataCriacao = reader.GetDateTime(reader.GetOrdinal("data_criacao")),
                                DataAtualizacao = reader.GetDateTime(reader.GetOrdinal("data_atualizacao"))
                            };
                        }
                    }
                }
            }

            // Retorna configuração padrão se não encontrar nenhuma
            return new ConfiguracaoEmail
            {
                ServidorSmtp = "smtp.gmail.com",
                Porta = 587,
                EmailRemetente = "",
                NomeRemetente = "Sistema Gerente",
                UsuarioSmtp = "",
                SenhaSmtp = "",
                SecurityMode = "None"
            };
        }

        private void SalvarConfiguracaoEmail(ConfiguracaoEmail configuracao)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                
                if (configuracao.Id > 0)
                {
                    // Atualizar configuração existente
                    using (var cmd = new NpgsqlCommand("UPDATE configuracoes_email SET servidor_smtp = @servidor, porta = @porta, email_remetente = @email, nome_remetente = @nome, usuario_smtp = @usuario, senha_smtp = @senha, security_mode = @securityMode, data_atualizacao = CURRENT_TIMESTAMP WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", configuracao.Id);
                        cmd.Parameters.AddWithValue("@servidor", string.IsNullOrEmpty(configuracao.ServidorSmtp) ? DBNull.Value : configuracao.ServidorSmtp);
                        cmd.Parameters.AddWithValue("@porta", configuracao.Porta);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(configuracao.EmailRemetente) ? DBNull.Value : configuracao.EmailRemetente);
                        cmd.Parameters.AddWithValue("@nome", string.IsNullOrEmpty(configuracao.NomeRemetente) ? DBNull.Value : configuracao.NomeRemetente);
                        cmd.Parameters.AddWithValue("@usuario", string.IsNullOrEmpty(configuracao.UsuarioSmtp) ? DBNull.Value : configuracao.UsuarioSmtp);
                        cmd.Parameters.AddWithValue("@senha", string.IsNullOrEmpty(configuracao.SenhaSmtp) ? DBNull.Value : CryptoUtils.Encrypt(configuracao.SenhaSmtp));
                        cmd.Parameters.AddWithValue("@securityMode", string.IsNullOrEmpty(configuracao.SecurityMode) ? "None" : configuracao.SecurityMode);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Inserir nova configuração
                    using (var cmd = new NpgsqlCommand("INSERT INTO configuracoes_email (servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp, security_mode) VALUES (@servidor, @porta, @email, @nome, @usuario, @senha, @securityMode)", conn))
                    {
                        cmd.Parameters.AddWithValue("@servidor", string.IsNullOrEmpty(configuracao.ServidorSmtp) ? DBNull.Value : configuracao.ServidorSmtp);
                        cmd.Parameters.AddWithValue("@porta", configuracao.Porta);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(configuracao.EmailRemetente) ? DBNull.Value : configuracao.EmailRemetente);
                        cmd.Parameters.AddWithValue("@nome", string.IsNullOrEmpty(configuracao.NomeRemetente) ? DBNull.Value : configuracao.NomeRemetente);
                        cmd.Parameters.AddWithValue("@usuario", string.IsNullOrEmpty(configuracao.UsuarioSmtp) ? DBNull.Value : configuracao.UsuarioSmtp);
                        cmd.Parameters.AddWithValue("@senha", string.IsNullOrEmpty(configuracao.SenhaSmtp) ? DBNull.Value : CryptoUtils.Encrypt(configuracao.SenhaSmtp));
                        cmd.Parameters.AddWithValue("@securityMode", string.IsNullOrEmpty(configuracao.SecurityMode) ? "None" : configuracao.SecurityMode);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
} 