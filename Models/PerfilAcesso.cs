using System.ComponentModel.DataAnnotations;

namespace Gerente.Models
{
    public class PerfilAcesso
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome do perfil é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Perfil")]
        public string Nome { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;
        
        [Display(Name = "Acesso a Configurações")]
        public bool AcessoConfiguracoes { get; set; }
        
        [Display(Name = "Acesso a Usuários")]
        public bool AcessoUsuarios { get; set; }
        
        [Display(Name = "Acesso a Projetos")]
        public bool AcessoProjetos { get; set; }
        
        [Display(Name = "Acesso a Backlog Arquitetura e Projetos")]
        public bool AcessoBacklogArquitetura { get; set; }
        
        [Display(Name = "Acesso a Relatórios")]
        public bool AcessoRelatorios { get; set; }
        
        [Display(Name = "Acesso Total")]
        public bool AcessoTotal { get; set; }
        
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
        
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }
        
        [Display(Name = "Data de Alteração")]
        public DateTime DataAlteracao { get; set; }
    }

    public class PerfilAcessoViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome do perfil é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Perfil")]
        public string Nome { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;
        
        [Display(Name = "Acesso a Configurações")]
        public bool AcessoConfiguracoes { get; set; }
        
        [Display(Name = "Acesso a Usuários")]
        public bool AcessoUsuarios { get; set; }
        
        [Display(Name = "Acesso a Projetos")]
        public bool AcessoProjetos { get; set; }
        
        [Display(Name = "Acesso a Backlog Arquitetura e Projetos")]
        public bool AcessoBacklogArquitetura { get; set; }
        
        [Display(Name = "Acesso a Relatórios")]
        public bool AcessoRelatorios { get; set; }
        
        [Display(Name = "Acesso Total")]
        public bool AcessoTotal { get; set; }
        
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
} 