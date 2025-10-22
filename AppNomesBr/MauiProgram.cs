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

            // Esta linha já deve registrar o IHttpClientFactory indiretamente
            NativeInjector.RegisterServices(builder.Services, startup.Configuration);
            RegisterPages(builder.Services);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // --- Mantenha o método RegisterPages e a classe Startup como estão ---
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