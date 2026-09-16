using GameCatalog.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameCatalog.API.Domain.Entities
{
    [Table("tb_jogo")]
    //Indice de apoio a busca textual e a ordenacao padrao da listagem
    [Index(nameof(Titulo), Name = "IDX_JOGO_TITULO")]
    //Indice composto: jogos de um estudio ordenados por lancamento
    [Index(nameof(EstudioId), nameof(DataLancamento), Name = "IDX_JOGO_ESTUDIO_DATA")]
    //Indice composto: filtro por genero combinado com faixa de preco
    [Index(nameof(Genero), nameof(Preco), Name = "IDX_JOGO_GENERO_PRECO")]
    public class JogoEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("s_titulo")]
        [StringLength(150, MinimumLength = 2)]
        public string Titulo { get; set; } = string.Empty;

        [Column("i_genero")]
        public GeneroJogo Genero { get; set; }

        [Column("n_preco")]
        public double Preco { get; set; }

        [Column("d_lancamento")]
        public DateTime DataLancamento { get; set; }

        [Column("n_nota")]
        public double Nota { get; set; }

        [ForeignKey(nameof(EstudioEntity))]
        [Column("i_estudio_id")]
        public int EstudioId { get; set; }

        //N:1 - ignorado no JSON para evitar ciclo de serializacao
        [JsonIgnore]
        public EstudioEntity? Estudio { get; set; }

        //N:N
        public ICollection<PlataformaEntity>? Plataformas { get; set; } = [];
    }
}
