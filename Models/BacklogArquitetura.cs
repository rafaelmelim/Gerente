using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Gerente.Models
{
    public class BacklogArquitetura
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A descrição da tarefa é obrigatória")]
        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
        [Display(Name = "Descrição da Tarefa")]
        public string DescricaoTarefa { get; set; } = string.Empty;

        [Required(ErrorMessage = "A prioridade é obrigatória")]
        [Display(Name = "Prioridade")]
        public string Prioridade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ganho é obrigatório")]
        [Display(Name = "Ganho")]
        public string Ganho { get; set; } = string.Empty;

        [Required(ErrorMessage = "O usuário é obrigatório")]
        [Display(Name = "Usuário")]
        public int UsuarioId { get; set; }

        [Display(Name = "Usuário")]
        public string? UsuarioNome { get; set; }

        [Display(Name = "Data Início")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }

        [Display(Name = "Data Fim")]
        [DataType(DataType.Date)]
        public DateTime? DataFim { get; set; }

        [Required(ErrorMessage = "O progresso é obrigatório")]
        [Display(Name = "Progresso")]
        public string Progresso { get; set; } = "Não Iniciado";

        [Display(Name = "Ambientes")]
        public string? Ambientes { get; set; }

        [StringLength(4000, ErrorMessage = "As anotações devem ter no máximo 4000 caracteres")]
        [Display(Name = "Anotações")]
        public string? Anotacoes { get; set; }

        [Display(Name = "Anexos")]
        public string? Anexos { get; set; }

        [Display(Name = "Data de Conclusão")]
        public DateTime? DataConclusao { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }

        [Display(Name = "Data de Alteração")]
        public DateTime DataAlteracao { get; set; }
    }

    public class BacklogArquiteturaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A descrição da tarefa é obrigatória")]
        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
        [Display(Name = "Descrição da Tarefa")]
        public string DescricaoTarefa { get; set; } = string.Empty;

        [Required(ErrorMessage = "A prioridade é obrigatória")]
        [Display(Name = "Prioridade")]
        public string Prioridade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O ganho é obrigatório")]
        [Display(Name = "Ganho")]
        public string Ganho { get; set; } = string.Empty;

        [Required(ErrorMessage = "O usuário é obrigatório")]
        [Display(Name = "Usuário")]
        public int UsuarioId { get; set; }

        [Display(Name = "Data Início")]
        [DataType(DataType.Date)]
        public DateTime? DataInicio { get; set; }

        [Display(Name = "Data Fim")]
        [DataType(DataType.Date)]
        public DateTime? DataFim { get; set; }

        [Required(ErrorMessage = "O progresso é obrigatório")]
        [Display(Name = "Progresso")]
        public string Progresso { get; set; } = "Não Iniciado";

        [Display(Name = "Ambientes")]
        public List<string>? AmbientesSelecionados { get; set; }

        [StringLength(4000, ErrorMessage = "As anotações devem ter no máximo 4000 caracteres")]
        [Display(Name = "Anotações")]
        public string? Anotacoes { get; set; }

        [Display(Name = "Anexos")]
        public IFormFileCollection? Anexos { get; set; }

        [Display(Name = "Comentários")]
        public string? NovoComentario { get; set; }
    }

    public class ComentarioBacklog
    {
        public int Id { get; set; }
        public int BacklogId { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public int UsuarioId { get; set; }
        public string? UsuarioNome { get; set; }
    }

    public class AnexoBacklog
    {
        public int Id { get; set; }
        public int BacklogId { get; set; }
        
        [Display(Name = "Nome do Arquivo")]
        public string NomeArquivo { get; set; } = string.Empty;
        
        [Display(Name = "Nome Original")]
        public string NomeOriginal { get; set; } = string.Empty;
        
        [Display(Name = "Caminho do Arquivo")]
        public string CaminhoArquivo { get; set; } = string.Empty;
        
        [Display(Name = "Tamanho (bytes)")]
        public long TamanhoBytes { get; set; }
        
        [Display(Name = "Tipo MIME")]
        public string? TipoMime { get; set; }
        
        [Display(Name = "Usuário")]
        public int UsuarioId { get; set; }
        
        [Display(Name = "Nome do Usuário")]
        public string? UsuarioNome { get; set; }
        
        [Display(Name = "Data de Upload")]
        public DateTime DataUpload { get; set; }
        
        [Display(Name = "Tamanho Formatado")]
        public string TamanhoFormatado { get; set; } = string.Empty;
    }

    public class ParametroGanho
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; }
    }

    public class ParametroAmbiente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; }
    }
} 