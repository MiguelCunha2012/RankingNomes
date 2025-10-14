using AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Infrastructure.Repositories;
using AppNomesBr.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppNomesBr.Tests.Integrados
{
    public class NomesBrServiceTests
    {
        private NomesApi apiIbge;
        private HttpClient httpClient;
        private NomesBrRepository nomesBrRepository;
        private NomesBrService nomesBrService;

        [SetUp]
        public void Setup()
        {
            httpClient = new() { BaseAddress = new Uri("https://servicodados.ibge.gov.br") };
            apiIbge = new(httpClient);

            var mock = new Mock<ILogger<NomesBrService>>();
            ILogger<NomesBrService> logger = mock.Object;

            var inMemorySettings = new Dictionary<string, string> { { "DbName", "local_db.db3" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            nomesBrRepository = new NomesBrRepository(configuration);

            nomesBrService = new NomesBrService(apiIbge, logger, nomesBrRepository);
        }

        [TearDown]
        public void TearDown()
        {
            httpClient.Dispose();
        }

        [Test]
        public async Task TestandoConsultarRegistros()
        {
            var results = await nomesBrRepository.GetAll();

            Assert.That(results?.Count, !Is.EqualTo(0));
        }

        [Test]
        public async Task TestandoIncluirNovoCadastro()
        {
            string nome = "Francisco";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Cristina";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Rodolfo";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Enzo";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Roberto";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Rogerio";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Vera";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Adriana";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Leandro";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Leandra";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Guilherme";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Renato";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Douglas";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Eduardo";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            nome = "Yago";

            await nomesBrService.InserirNovoRegistroNoRanking(nome);

            Assert.Pass();
        }

        [Test]
        public async Task ExcluindoTodosOsRegistros()
        {
            var registros = await nomesBrRepository.GetAll();

            foreach (var registro in registros)
                await nomesBrRepository.Delete(registro.Id);

            registros = await nomesBrRepository.GetAll();

            Assert.That(registros, Is.Empty);
        }
    }
}
