using System.ComponentModel.DataAnnotations;

namespace GameCatalog.API.Application.Dtos
{
    public class PostEstudioDto
    {
        [Required]
        [StringLength(150, MinimumLength = 3)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        public string Pais { get; set; } = string.Empty;

        [Range(1958, 2100)]
        public int AnoFundacao { get; set; }

        public bool Ativo { get; set; } = true;

        public IEnumerable<PostJogoDto>? Jogos { get; set; }
    }
}
