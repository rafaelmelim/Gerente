using System;
using System.Security.Cryptography;
using System.Text;
using Gerente.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Gerente.Services
{
    public class PasswordResetService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PasswordResetService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                // Verificar se o e-mail existe no sistema
                var userId = await GetUserIdByEmailAsync(email);
                if (userId == null)
                {
                    return false; // E-mail não encontrado
                }

                // Gerar token único
                var token = GenerateSecureToken();
                var expiresAt = DateTime.UtcNow.AddHours(24); // Token válido por 24 horas

                // Salvar token no banco
                await SaveTokenAsync(userId.Value, token, email, expiresAt);

                // Enviar e-mail
                await SendPasswordResetEmailAsync(email, token);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connString))
                {
                    return false;
                }

                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id FROM password_reset_tokens WHERE token = @token AND expires_at > @now AND used = false", conn))
                    {
                        cmd.Parameters.AddWithValue("@token", token);
                        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                        var result = await cmd.ExecuteScalarAsync();
                        return result != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connString))
                {
                    return false;
                }

                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();
                    
                    // Verificar se o token é válido e obter o user_id
                    int? userId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT user_id FROM password_reset_tokens WHERE token = @token AND expires_at > @now AND used = false", conn))
                    {
                        cmd.Parameters.AddWithValue("@token", token);
                        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            userId = Convert.ToInt32(result);
                        }
                    }

                    if (userId == null)
                    {
                        return false;
                    }

                    // Atualizar a senha do usuário (com hash)
                    var hashedPassword = HashPassword(newPassword);
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE usuarios SET senha = @senha WHERE id = @userId", conn))
                    {
                        cmd.Parameters.AddWithValue("@senha", hashedPassword);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Marcar token como usado
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE password_reset_tokens SET used = true WHERE token = @token", conn))
                    {
                        cmd.Parameters.AddWithValue("@token", token);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<int?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connString))
                {
                    return null;
                }

                using (var conn = new NpgsqlConnection(connString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id FROM usuarios WHERE email = @email", conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        var result = await cmd.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveTokenAsync(int userId, string token, string email, DateTime expiresAt)
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO password_reset_tokens (user_id, token, email, expires_at) VALUES (@userId, @token, @email, @expiresAt)", conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SendPasswordResetEmailAsync(string email, string token)
        {
            try
            {
                var configuracao = GetEmailConfiguration();
                if (configuracao == null)
                {
                    throw new InvalidOperationException("Configuração de e-mail não encontrada.");
                }

                var resetUrl = $"{GetBaseUrl()}/Login/ResetPassword?token={token}";
                var subject = "Redefinição de Senha - Sistema DorowCamp";
                var body = GenerateEmailBody(resetUrl);

                using (var client = new System.Net.Mail.SmtpClient(configuracao.ServidorSmtp, configuracao.Porta))
                {
                    client.EnableSsl = configuracao.SecurityMode == "SSL" || configuracao.SecurityMode == "TLS";
                    client.Credentials = new System.Net.NetworkCredential(configuracao.UsuarioSmtp, configuracao.SenhaSmtp);
                    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;

                    var message = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(configuracao.EmailRemetente, configuracao.NomeRemetente),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    message.To.Add(email);

                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                // Log do erro (em produção, usar um sistema de logging adequado)
                Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
                throw;
            }
        }

        private ConfiguracaoEmail? GetEmailConfiguration()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connString))
                {
                    return null;
                }

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp, security_mode FROM configuracoes_email ORDER BY id DESC LIMIT 1", conn))
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
                                    SecurityMode = reader.IsDBNull(reader.GetOrdinal("security_mode")) ? "None" : reader.GetString(reader.GetOrdinal("security_mode"))
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string HashPassword(string password)
        {
            // Em produção, use um algoritmo de hash mais seguro como BCrypt ou Argon2
            // Por simplicidade, aqui estamos usando SHA256
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GetBaseUrl()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var request = httpContext.Request;
                    var host = request.Host.Host;
                    var port = request.Host.Port?.ToString() ?? "";
                    
                    // Se não há porta especificada, usar a porta padrão baseada no protocolo
                    if (string.IsNullOrEmpty(port))
                    {
                        port = request.IsHttps ? "443" : "80";
                    }
                    
                    // Se a porta é 80 (HTTP) ou 443 (HTTPS), não incluir no URL
                    var portSegment = (port == "80" || port == "443") ? "" : $":{port}";
                    
                    return $"{request.Scheme}://{host}{portSegment}";
                }
                
                // Fallback se não conseguir obter o contexto HTTP
                return "http://localhost:5144";
            }
            catch (Exception ex)
            {
                // Log do erro (em produção, usar um sistema de logging adequado)
                Console.WriteLine($"Erro ao obter URL base da aplicação: {ex.Message}");
                // Fallback para configuração padrão
                return "http://localhost:5144";
            }
        }

        private string GenerateEmailBody(string resetUrl)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Sistema DorowCamp</h1>
                        </div>
                        <div class='content'>
                            <h2>Redefinição de Senha</h2>
                            <p>Você solicitou a redefinição de sua senha no Sistema DorowCamp.</p>
                            <p>Clique no botão abaixo para criar uma nova senha:</p>
                            <div style='text-align: center;'>
                                <a href='{resetUrl}' class='button'>Redefinir Senha</a>
                            </div>
                            <p><strong>Importante:</strong></p>
                            <ul>
                                <li>Este link é válido por 24 horas</li>
                                <li>Se você não solicitou esta redefinição, ignore este e-mail</li>
                                <li>Por segurança, este link só pode ser usado uma vez</li>
                            </ul>
                            <p>Se o botão não funcionar, copie e cole o link abaixo no seu navegador:</p>
                            <p style='word-break: break-all; color: #667eea;'>{resetUrl}</p>
                        </div>
                        <div class='footer'>
                            <p>Este é um e-mail automático, não responda a esta mensagem.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
} 