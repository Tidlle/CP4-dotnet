using System.ComponentModel.DataAnnotations;

namespace GameCatalog.API.Application.Dtos
{
    public class PostPlataformaDto
    {
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
    }
}
