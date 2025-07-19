using System;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;

namespace Gerente.Services
{
    public class UsuarioAtivacaoService
    {
        private readonly IConfiguration _configuration;

        public UsuarioAtivacaoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailAtivacaoAsync(string emailUsuario, string nomeUsuario, string? novaSenha)
        {
            Console.WriteLine("=== INICIANDO ENVIO DE EMAIL DE ATIVAÇÃO ===");
            Console.WriteLine($"Email: {emailUsuario}");
            Console.WriteLine($"Nome: {nomeUsuario}");
            Console.WriteLine($"Nova senha (original): {novaSenha}");
            Console.WriteLine($"Tamanho da senha: {novaSenha?.Length ?? 0}");
            Console.WriteLine($"Senha é nula: {novaSenha == null}");
            Console.WriteLine($"Senha está vazia: {string.IsNullOrEmpty(novaSenha)}");

            try
            {
                var configuracao = ObterConfiguracaoEmail();
                if (configuracao == null)
                {
                    Console.WriteLine("ERRO: Configuração de e-mail não encontrada!");
                    throw new InvalidOperationException("Configuração de e-mail não encontrada.");
                }

                Console.WriteLine($"=== CONFIGURAÇÃO SMTP OBTIDA ===");
                Console.WriteLine($"Servidor: {configuracao.ServidorSmtp}");
                Console.WriteLine($"Porta: {configuracao.Porta}");
                Console.WriteLine($"Usuário: {configuracao.UsuarioSmtp}");
                Console.WriteLine($"Remetente: {configuracao.EmailRemetente}");

                using (var client = new SmtpClient(configuracao.ServidorSmtp, configuracao.Porta))
                {
                    client.EnableSsl = configuracao.SecurityMode == "SSL" || configuracao.SecurityMode == "TLS";
                    client.Credentials = new NetworkCredential(configuracao.UsuarioSmtp, configuracao.SenhaSmtp);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var message = new MailMessage
                    {
                        From = new MailAddress(configuracao.EmailRemetente, configuracao.NomeRemetente),
                        Subject = "Conta Ativada - No Sistema",
                        Body = GerarCorpoEmail(nomeUsuario, novaSenha ?? ""),
                        IsBodyHtml = true
                    };

                    message.To.Add(emailUsuario);

                    Console.WriteLine("Enviando email...");
                    await client.SendMailAsync(message);
                    Console.WriteLine("Email enviado com sucesso!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO AO ENVIAR EMAIL: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private string GerarCorpoEmail(string nomeUsuario, string novaSenha)
        {
            Console.WriteLine($"=== GERANDO CORPO DO EMAIL ===");
            Console.WriteLine($"Nome do usuário: {nomeUsuario}");
            Console.WriteLine($"Nova senha para email (criptografada): {novaSenha}");
            Console.WriteLine($"Tamanho da senha para email: {novaSenha?.Length ?? 0}");
            
            // Processar a senha - tentar descriptografar se possível
            string senhaDescriptografada = novaSenha ?? "";
            try
            {
                if (!string.IsNullOrEmpty(novaSenha))
                {
                    // Tentar descriptografar a senha
                    string resultado = CryptoUtils.Decrypt(novaSenha);
                    
                    // Se a descriptografia retornou string vazia, usar a senha original
                    if (string.IsNullOrEmpty(resultado))
                    {
                        senhaDescriptografada = novaSenha;
                        Console.WriteLine($"Descriptografia retornou vazio, usando senha original: {senhaDescriptografada}");
                    }
                    else
                    {
                        senhaDescriptografada = resultado;
                        Console.WriteLine($"Senha descriptografada com sucesso: {senhaDescriptografada}");
                    }
                    
                    Console.WriteLine($"Tamanho da senha final: {senhaDescriptografada?.Length ?? 0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO ao descriptografar senha: {ex.Message}");
                Console.WriteLine("Usando senha original (não criptografada)");
                senhaDescriptografada = novaSenha ?? "";
            }
            
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f8f9fa; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                        .senha {{ background-color: #e9ecef; padding: 10px; border-radius: 5px; font-family: monospace; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Sistema</h2>
                        </div>
                        <div class='content'>
                            <h3>Olá {nomeUsuario},</h3>
                            <p>Sua conta foi <strong>ativada com sucesso</strong> no Sistema!</p>
                            <p>Agora você pode acessar o sistema usando suas credenciais:</p>
                            <p><strong>Nova senha de acesso:</strong></p>
                            <div class='senha'>{senhaDescriptografada}</div>
                            <p><strong>Importante:</strong> Guarde esta senha em local seguro e altere-a após o primeiro login.</p>
                            <p>Se você não solicitou esta ativação, entre em contato com o administrador do sistema.</p>
                        </div>
                        <div class='footer'>
                            <p>Este é um e-mail automático do sistema.</p>
                            <p>Não responda a este e-mail.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private ConfiguracaoEmail? ObterConfiguracaoEmail()
        {
            Console.WriteLine("=== OBTENDO CONFIGURAÇÃO DE EMAIL ===");
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                Console.WriteLine("ERRO: Connection string não encontrada!");
                return null;
            }

            Console.WriteLine("Conectando ao banco de dados...");
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                Console.WriteLine("Conexão aberta, executando query...");
                
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp, security_mode FROM configuracoes_email ORDER BY id DESC LIMIT 1", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var config = new ConfiguracaoEmail
                            {
                                Id = reader.GetInt32(0),
                                ServidorSmtp = reader.GetString(1),
                                Porta = reader.GetInt32(2),
                                EmailRemetente = reader.GetString(3),
                                NomeRemetente = reader.GetString(4),
                                UsuarioSmtp = reader.GetString(5),
                                SenhaSmtp = reader.IsDBNull(6) ? "" : CryptoUtils.Decrypt(reader.GetString(6)),
                                SecurityMode = reader.IsDBNull(7) ? "None" : reader.GetString(7)
                            };
                            Console.WriteLine($"ID: {config.Id}, Servidor: {config.ServidorSmtp}");
                            return config;
                        }
                    }
                }
            }

            Console.WriteLine("ERRO: Configuração de e-mail não encontrada!");
            return null;
        }
    }
} 