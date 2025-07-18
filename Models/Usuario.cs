using System.ComponentModel.DataAnnotations;

namespace Gerente.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome do usuário é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Usuário")]
        public string Nome { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [StringLength(255, ErrorMessage = "O e-mail deve ter no máximo 255 caracteres")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
        [Display(Name = "Senha de Acesso")]
        public string Senha { get; set; } = string.Empty;
        
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }
        
        [Display(Name = "Data de Alteração")]
        public DateTime DataAlteracao { get; set; }
        
        [Display(Name = "Perfil de Acesso")]
        public int? PerfilAcessoId { get; set; }
        
        [Display(Name = "Perfil de Acesso")]
        public string? PerfilAcessoNome { get; set; }
    }

    public class UsuarioViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome do usuário é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Usuário")]
        public string Nome { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O e-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        [StringLength(255, ErrorMessage = "O e-mail deve ter no máximo 255 caracteres")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
        [Display(Name = "Senha de Acesso")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
        [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
        [Display(Name = "Confirmar Senha")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha { get; set; } = string.Empty;
        
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }
        
        [Display(Name = "Data de Alteração")]
        public DateTime DataAlteracao { get; set; }
        
        [Display(Name = "Perfil de Acesso")]
        public int? PerfilAcessoId { get; set; }
        
        [Display(Name = "Perfil de Acesso")]
        public string? PerfilAcessoNome { get; set; }
    }
} 