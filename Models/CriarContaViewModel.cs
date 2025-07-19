using System.ComponentModel.DataAnnotations;

namespace Gerente.Models
{
    public class CriarContaViewModel
    {
        [Required(ErrorMessage = "O nome do usuário é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Usuário")]
        public string Nome { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [StringLength(255, ErrorMessage = "O e-mail deve ter no máximo 255 caracteres")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
    }
} 