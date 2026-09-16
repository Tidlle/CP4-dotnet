using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameCatalog.API.Domain.Entities
{
    [Table("tb_estudio")]
    //Indice unico: acelera a busca por nome e garante que nao existam estudios duplicados
    [Index(nameof(Nome), IsUnique = true, Name = "IDX_ESTUDIO_NOME")]
    //Indice composto: filtro de listagem por pais e ano de fundacao
    [Index(nameof(Pais), nameof(AnoFundacao), Name = "IDX_ESTUDIO_PAIS_ANO")]
    public class EstudioEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("s_nome")]
        [StringLength(150, MinimumLength = 3)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Column("s_pais")]
        [StringLength(60)]
        public string Pais { get; set; } = string.Empty;

        [Column("i_ano_fundacao")]
        public int AnoFundacao { get; set; }

        [Column("b_ativo")]
        public bool Ativo { get; set; } = true;

        //1:N
        public ICollection<JogoEntity>? Jogos { get; set; }
    }
}
