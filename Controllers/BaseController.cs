using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Gerente.Models;
using Npgsql;

namespace Gerente.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly IConfiguration _configuration;

        protected BaseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            // Carregar parâmetros do sistema para todas as views
            var parametros = ObterParametrosSistema();
            if (parametros != null)
            {
                ViewBag.CabecalhoSistema = parametros.CabecalhoSistema;
                ViewBag.VersaoSistema = parametros.VersaoSistema;
                ViewBag.NomeRodape = parametros.NomeRodape;
            }
        }

        private ParametroSistema? ObterParametrosSistema()
        {
            string? connString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connString))
            {
                return null;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT id, cabecalho_sistema, versao_sistema, nome_rodape FROM parametros_sistema ORDER BY id LIMIT 1", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ParametroSistema
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    CabecalhoSistema = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    VersaoSistema = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    NomeRodape = reader.IsDBNull(3) ? "" : reader.GetString(3)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Retornar null em caso de erro
            }

            return null;
        }
    }
} 