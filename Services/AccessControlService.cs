using System;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Gerente.Models;

namespace Gerente.Services
{
    public class AccessControlService
    {
        private readonly IConfiguration _configuration;

        public AccessControlService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool HasAccess(int userId, string resource)
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
                    conn.Open();
                    
                    // Verificar se o usuário tem acesso total
                    using (var cmd = new NpgsqlCommand(
                        @"SELECT pa.acesso_total 
                          FROM usuarios u 
                          JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id 
                          WHERE u.id = @userId AND u.ativo = true", conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value && (bool)result)
                        {
                            return true; // Acesso total
                        }
                    }

                    // Verificar acesso específico
                    string accessColumn = GetAccessColumn(resource);
                    if (string.IsNullOrEmpty(accessColumn))
                    {
                        return false;
                    }

                    using (var cmd = new NpgsqlCommand(
                        $@"SELECT pa.{accessColumn} 
                           FROM usuarios u 
                           JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id 
                           WHERE u.id = @userId AND u.ativo = true", conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();
                        
                        return result != null && result != DBNull.Value && (bool)result;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Overloads para aceitar string como userId
        public bool HasAccess(string userIdStr, string resource)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccess(userId, resource);
            return false;
        }
        public bool HasAccessToConfigurations(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToConfigurations(userId);
            return false;
        }
        public bool HasAccessToUsers(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToUsers(userId);
            return false;
        }
        public bool HasAccessToProjects(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToProjects(userId);
            return false;
        }
        public bool HasAccessToBacklogArquitetura(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToBacklogArquitetura(userId);
            return false;
        }
        public bool HasAccessToReports(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToReports(userId);
            return false;
        }
        public bool HasAccessToSystemParameters(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasAccessToSystemParameters(userId);
            return false;
        }
        public bool HasTotalAccess(string userIdStr)
        {
            if (int.TryParse(userIdStr, out int userId))
                return HasTotalAccess(userId);
            return false;
        }

        // Métodos específicos para int
        public bool HasAccessToConfigurations(int userId)
        {
            return HasAccess(userId, "configuracoes");
        }

        public bool HasAccessToUsers(int userId)
        {
            return HasAccess(userId, "usuarios");
        }

        public bool HasAccessToProjects(int userId)
        {
            return HasAccess(userId, "projetos");
        }

        public bool HasAccessToBacklogArquitetura(int userId)
        {
            return HasAccess(userId, "backlog_arquitetura");
        }

        public bool HasAccessToReports(int userId)
        {
            return HasAccess(userId, "relatorios");
        }

        public bool HasAccessToSystemParameters(int userId)
        {
            return HasAccess(userId, "parametros_sistema");
        }

        public bool HasTotalAccess(int userId)
        {
            return HasAccess(userId, "total");
        }

        private string GetAccessColumn(string resource)
        {
            return resource.ToLower() switch
            {
                "configuracoes" => "acesso_configuracoes",
                "usuarios" => "acesso_usuarios",
                "projetos" => "acesso_projetos",
                "backlog_arquitetura" => "acesso_backlog_arquitetura",
                "relatorios" => "acesso_relatorios",
                "parametros_sistema" => "acesso_parametros_sistema",
                "total" => "acesso_total",
                _ => string.Empty
            };
        }

        public PerfilAcesso? GetUserProfile(int userId)
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
                        @"SELECT pa.id, pa.nome, pa.descricao, pa.acesso_configuracoes, 
                                 pa.acesso_usuarios, pa.acesso_projetos, pa.acesso_backlog_arquitetura,
                                 pa.acesso_relatorios, pa.acesso_total, pa.ativo
                          FROM usuarios u 
                          JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id 
                          WHERE u.id = @userId AND u.ativo = true", conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
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
                                    AcessoBacklogArquitetura = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                                    AcessoRelatorios = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                                    AcessoTotal = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                                    Ativo = reader.IsDBNull(9) ? false : reader.GetBoolean(9)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log error if needed
            }

            return null;
        }
    }
} 