using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Interfaces.Services;
using AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Infrastructure.Repositories;
using AppNomesBr.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppNomesBr.Infrastructure.IoC
{
    public static class NativeInjector
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            #region Services

            services.AddSingleton<INomesBrService, NomesBrService>();

            #endregion

            #region Repositories

            services.AddScoped<INomesBrRepository, NomesBrRepository>();

            #endregion

            #region External Integrations

            services.AddHttpClient<INomesApi, NomesApi>(
                nameof(NomesApi),
                client =>
            {
                var uri = configuration["ExternalIntegrations:IBGE:Censos:NomesApi"] ?? string.Empty;
                client.BaseAddress = new Uri(uri);
            });

            #endregion
        }
    }
}
