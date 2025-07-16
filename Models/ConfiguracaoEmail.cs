namespace Gerente.Models
{
    public class ConfiguracaoEmail
    {
        public int Id { get; set; }
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Porta { get; set; }
        public string EmailRemetente { get; set; } = string.Empty;
        public string NomeRemetente { get; set; } = string.Empty;
        public string UsuarioSmtp { get; set; } = string.Empty;
        public string SenhaSmtp { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
} 