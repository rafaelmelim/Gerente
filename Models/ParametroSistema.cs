using System.ComponentModel.DataAnnotations;

namespace Gerente.Models
{
    public class ParametroSistema
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O cabeçalho do sistema é obrigatório")]
        [StringLength(200, ErrorMessage = "O cabeçalho deve ter no máximo 200 caracteres")]
        [Display(Name = "Cabeçalho do Sistema")]
        public string CabecalhoSistema { get; set; } = string.Empty;

        [Required(ErrorMessage = "A versão do sistema é obrigatória")]
        [StringLength(50, ErrorMessage = "A versão deve ter no máximo 50 caracteres")]
        [Display(Name = "Versão do Sistema")]
        public string VersaoSistema { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome do rodapé é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome do rodapé deve ter no máximo 200 caracteres")]
        [Display(Name = "Nome do Rodapé")]
        public string NomeRodape { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }
        public DateTime DataAlteracao { get; set; }
    }
} 