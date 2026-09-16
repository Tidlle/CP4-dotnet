using GameCatalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameCatalog.API.Infrastructure.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {

        }

        public DbSet<EstudioEntity> Estudio { get; set; }

        public DbSet<JogoEntity> Jogo { get; set; }

        public DbSet<PlataformaEntity> Plataforma { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Nome curto para a tabela de ligacao N:N entre jogo e plataforma
            modelBuilder
                .Entity<JogoEntity>()
                .HasMany(x => x.Plataformas)
                .WithMany(x => x.Jogos)
                .UsingEntity(j => j.ToTable("tb_jogo_plataforma"));

            base.OnModelCreating(modelBuilder);
        }
    }
}
