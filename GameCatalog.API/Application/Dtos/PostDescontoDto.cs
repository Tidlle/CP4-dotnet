using System.ComponentModel.DataAnnotations;

namespace GameCatalog.API.Application.Dtos
{
    public class PostDescontoDto
    {
        /// <summary>
        /// Percentual de desconto aplicado sobre o preco atual do jogo.
        /// </summary>
        [Range(0.01, 90)]
        public double Percentual { get; set; }
    }
}
