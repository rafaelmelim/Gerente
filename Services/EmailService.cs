using System;
using System.Net.Mail;
using System.Net;
using Gerente.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Gerente.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarEmailConfirmacaoCadastroAsync(string emailUsuario, string nomeUsuario)
        {
            try
            {
                Console.WriteLine($"=== INICIANDO ENVIO DE EMAIL DE CONFIRMAÇÃO ===");
                Console.WriteLine($"Email: {emailUsuario}");
                Console.WriteLine($"Nome: {nomeUsuario}");
                
                var configuracao = ObterConfiguracaoEmail();
                if (configuracao == null)
                {
                    Console.WriteLine("ERRO: Configuração de e-mail não encontrada!");
                    throw new InvalidOperationException("Configuração de e-mail não encontrada.");
                }

                Console.WriteLine($"Configuração encontrada:");
                Console.WriteLine($"  Servidor: {configuracao.ServidorSmtp}:{configuracao.Porta}");
                Console.WriteLine($"  Usuário: {configuracao.UsuarioSmtp}");
                Console.WriteLine($"  Remetente: {configuracao.EmailRemetente}");
                Console.WriteLine($"  Security Mode: {configuracao.SecurityMode}");

                var subject = "Cadastro Realizado - Sistema DorowCamp";
                var body = GerarCorpoEmailConfirmacao(nomeUsuario);

                Console.WriteLine("Configurando cliente SMTP...");
                using (var client = new System.Net.Mail.SmtpClient(configuracao.ServidorSmtp, configuracao.Porta))
                {
                    client.EnableSsl = configuracao.SecurityMode == "SSL" || configuracao.SecurityMode == "TLS";
                    client.Credentials = new System.Net.NetworkCredential(configuracao.UsuarioSmtp, configuracao.SenhaSmtp);
                    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    client.Timeout = 30000; // 30 segundos

                    Console.WriteLine("Criando mensagem...");
                    var message = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(configuracao.EmailRemetente, configuracao.NomeRemetente),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    message.To.Add(emailUsuario);

                    Console.WriteLine("Enviando email...");
                    await client.SendMailAsync(message);
                    Console.WriteLine("EMAIL ENVIADO COM SUCESSO!");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO AO ENVIAR EMAIL: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> EnviarEmailNotificacaoAdminAsync(string emailUsuario, string nomeUsuario)
        {
            try
            {
                var configuracao = ObterConfiguracaoEmail();
                if (configuracao == null)
                {
                    throw new InvalidOperationException("Configuração de e-mail não encontrada.");
                }

                var subject = "Novo Usuário Cadastrado - Sistema DorowCamp";
                var body = GerarCorpoEmailNotificacaoAdmin(nomeUsuario, emailUsuario);

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
                    message.To.Add(configuracao.EmailRemetente);

                    await client.SendMailAsync(message);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                // Log do erro (em produção, usar um sistema de logging adequado)
                Console.WriteLine($"Erro ao enviar e-mail de notificação admin: {ex.Message}");
                throw;
            }
        }



        private ConfiguracaoEmail? ObterConfiguracaoEmail()
        {
            try
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
                                Console.WriteLine("Configuração encontrada no banco!");
                                var config = new ConfiguracaoEmail
                                {
                                    Id = reader.GetInt32(0),
                                    ServidorSmtp = reader.GetString(1),
                                    Porta = reader.GetInt32(2),
                                    EmailRemetente = reader.GetString(3),
                                    NomeRemetente = reader.GetString(4),
                                    UsuarioSmtp = reader.GetString(5),
                                    SenhaSmtp = CryptoUtils.Decrypt(reader.GetString(6)),
                                    SecurityMode = reader.IsDBNull(7) ? "None" : reader.GetString(7)
                                };
                                Console.WriteLine($"ID: {config.Id}, Servidor: {config.ServidorSmtp}");
                                return config;
                            }
                            else
                            {
                                Console.WriteLine("NENHUMA CONFIGURAÇÃO ATIVA ENCONTRADA NO BANCO!");
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO AO OBTER CONFIGURAÇÃO: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        private string GerarCorpoEmailConfirmacao(string nomeUsuario)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f8f9fa; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Sistema DorowCamp</h1>
                        </div>
                        <div class='content'>
                            <h2>Cadastro Realizado com Sucesso!</h2>
                            <p>Olá <strong>{nomeUsuario}</strong>,</p>
                            <p>Seu cadastro foi realizado com sucesso e está aguardando ativação por um administrador.</p>
                            <p>Você receberá uma notificação quando sua conta for ativada.</p>
                            <p>Agradecemos sua paciência!</p>
                        </div>
                        <div class='footer'>
                            <p>Este é um e-mail automático, não responda a esta mensagem.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GerarCorpoEmailNotificacaoAdmin(string nomeUsuario, string emailUsuario)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f8f9fa; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .info {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Novo Usuário Cadastrado</h1>
                        </div>
                        <div class='content'>
                            <h2>Atenção Administrador!</h2>
                            <p>Um novo usuário foi cadastrado no sistema e está aguardando validação/ativação.</p>
                            
                            <div class='info'>
                                <strong>Informações do Usuário:</strong><br>
                                <strong>Nome:</strong> {nomeUsuario}<br>
                                <strong>E-mail:</strong> {emailUsuario}<br>
                                <strong>Data do Cadastro:</strong> {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}
                            </div>
                            
                            <p><strong>Ação Necessária:</strong> Favor verificar o painel de usuários e ativar a conta se necessário.</p>
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