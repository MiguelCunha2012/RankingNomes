using AppNomesBr.Infrastructure.IoC;
using AppNomesBr.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AppNomesBr
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            var startup = new Startup();
            builder.Configuration.AddConfiguration(startup.Configuration);
            builder.Services.AddLogging();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            NativeInjector.RegisterServices(builder.Services, startup.Configuration);
            RegisterPages(builder.Services);

            #if DEBUG
            builder.Logging.AddDebug();
            #endif

            return builder.Build();
        }

        /// <summary>
        /// Método responsável por fazer a injeção de dependencia das páginas da aplicação
        /// <para>
        /// Existem três formas de resolver dependências no ASP.NET, AddTransient, AddScoped e AddSingleton, e todas estão ligadas ao tempo de vida do objeto resolvido.
        /// </para>
        /// <para>
        /// <b>AddSingleton:</b> Sempre teremos as mesmas informações (Mesma instância) do objeto para todos os usuários da aplicação. As aplicações ASP.NET ficam em constante execução, atendendo diferentes usuários, então muito cuidado ao utilizar o Singleton, já que os dados serão compartilhados entre todas as requisições. Um bom exemplo de uso são as configurações da aplicação (Desde que não sejam por usuário), que podem ser carregadas uma única vez no início da aplicação e reutilizada posteriormente.
        /// </para>
        /// <para>
        /// <b>AddTransient:</b> Sempre teremos uma nova instância do objeto. Este cenário é ideal para quando queremos executar ações pontuais e já dispor o objeto, que normalmente é a maioria dos casos.
        /// </para>
        /// <para>
        /// <b>AddScoped:</b> O AddScoped trabalha de forma parecida com o AddTransient porém ele retém o objeto durante toda a requisição, e sempre que invocado, retorna o mesmo objeto. Isto termina no fim da requisição. Após enviar os dados para o client, o ASP.NET se encarrega de remover os objetos da memória.
        /// </para>
        /// </summary>
        /// <param name="services"></param>
        public static void RegisterPages(IServiceCollection services)
        {
            #region Singleton

            #endregion

            #region Transient

            services.AddTransient<RankingNomesBrasileiros>();
            services.AddTransient<NovaConsultaNome>();

            #endregion

            #region Scoped

            #endregion
        }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("AppNomesBr.appsettings.json") ?? Stream.Null;
            Configuration = new ConfigurationBuilder()
                .AddJsonStream(stream).Build();
        }
    }
}
