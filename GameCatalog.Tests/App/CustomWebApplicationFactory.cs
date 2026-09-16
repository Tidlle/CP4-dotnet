using GameCatalog.API.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace GameCatalog.Tests.App
{
    /// <summary>
    /// Sobe a API completa em memoria (TestServer) trocando os use cases por mocks.
    /// Todo o pipeline real e exercitado: controllers, rate limiting e compressao.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IEstudioUseCase> EstudioUseCaseMock { get; set; } = new();

        public Mock<IJogoUseCase> JogoUseCaseMock { get; set; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IEstudioUseCase));
                services.AddSingleton(EstudioUseCaseMock.Object);

                services.RemoveAll(typeof(IJogoUseCase));
                services.AddSingleton(JogoUseCaseMock.Object);

                services.AddSingleton<IStartupFilter, CompatibilidadeTestServerFilter>();
            });
        }

        /// <summary>
        /// Compatibilidade do TestServer com runtimes mais novos que o .NET 8.
        ///
        /// O PipeWriter do Microsoft.AspNetCore.TestHost 8.x nao implementa
        /// PipeWriter.UnflushedBytes, membro que o System.Text.Json passou a exigir
        /// a partir do .NET 9. Em uma maquina que possua apenas um runtime mais novo
        /// instalado, toda resposta JSON falharia com InvalidOperationException.
        ///
        /// Este filtro troca a feature de corpo da resposta por uma implementacao
        /// baseada em Stream, compativel com as duas versoes. E codigo exclusivo dos
        /// testes: a API em execucao (Kestrel) nao e afetada.
        /// </summary>
        private sealed class CompatibilidadeTestServerFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, proximo) =>
                {
                    var featureOriginal = context.Features.Get<IHttpResponseBodyFeature>();

                    if (featureOriginal is null)
                    {
                        await proximo(context);
                        return;
                    }

                    var featureCompativel = new StreamResponseBodyFeature(featureOriginal.Stream);
                    context.Features.Set<IHttpResponseBodyFeature>(featureCompativel);

                    try
                    {
                        await proximo(context);
                        await featureCompativel.CompleteAsync();
                    }
                    finally
                    {
                        context.Features.Set(featureOriginal);
                    }
                });

                next(app);
            };
        }
    }
}
