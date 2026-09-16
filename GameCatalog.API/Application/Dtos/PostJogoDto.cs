using GameCatalog.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameCatalog.API.Application.Dtos
{
    public class PostJogoDto
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public GeneroJogo Genero { get; set; }

        [Range(0, 1000)]
        public double Preco { get; set; }

        public DateTime DataLancamento { get; set; }

        [Range(0, 10)]
        public double Nota { get; set; }

        public int EstudioId { get; set; }

        public IEnumerable<PostPlataformaDto>? Plataformas { get; set; }
    }
}
