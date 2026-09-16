using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameCatalog.API.Domain.Entities
{
    [Table("tb_plataforma")]
    public class PlataformaEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("s_nome")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        //N:N
        [JsonIgnore]
        public ICollection<JogoEntity> Jogos { get; set; } = [];
    }
}
