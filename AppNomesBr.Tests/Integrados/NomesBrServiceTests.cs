using AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Infrastructure.Repositories;
using AppNomesBr.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework; 
using System; 
using System.Collections.Generic; 
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppNomesBr.Tests.Integrados
{
    public class NomesBrServiceTests
    {
        private NomesApi apiIbge;
        private HttpClient httpClient;
        private NomesBrRepository nomesBrRepository;
        private NomesBrService nomesBrService;
        private Mock<IHttpClientFactory> mockHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            httpClient = new() { BaseAddress = new Uri("https://servicodados.ibge.gov.br") };
            apiIbge = new(httpClient);

            var mockLogger = new Mock<ILogger<NomesBrService>>(); 
            ILogger<NomesBrService> logger = mockLogger.Object;

            var inMemorySettings = new Dictionary<string, string> { { "DbName", "local_db_test.db3" } }; 
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            nomesBrRepository = new NomesBrRepository(configuration);

            mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
          


            nomesBrService = new NomesBrService(apiIbge, logger, nomesBrRepository, mockHttpClientFactory.Object);

         
        }

        [TearDown]
        public void TearDown()
        {
            httpClient?.Dispose();
        }

        [Test]
        public async Task TestandoConsultarRegistros()
        {

            await ExcluindoTodosOsRegistrosInternal(); 
            await nomesBrService.InserirNovoRegistroNoRanking("TesteConsultaUnica");

            var results = await nomesBrRepository.GetAll();


            Assert.That(results?.Count, Is.GreaterThan(0));
            Assert.That(results?.Any(r => r.Nome == "TesteConsultaUnica"), Is.True);
        }


        [Test]
        public async Task TestandoIncluirNovoCadastroERanking() 
        {
            await ExcluindoTodosOsRegistrosInternal();

            string nomeFrancisco = "Francisco";
            string nomeCristina = "Cristina"; 

            await nomesBrService.InserirNovoRegistroNoRanking(nomeFrancisco);
            var registrosAposFrancisco = await nomesBrRepository.GetAll();
            var registroFrancisco = registrosAposFrancisco.FirstOrDefault(n => n.Nome == nomeFrancisco);
            Assert.That(registroFrancisco, Is.Not.Null, $"{nomeFrancisco} não foi inserido.");
            Assert.That(registroFrancisco?.Ranking, Is.EqualTo(1), $"Ranking inicial de {nomeFrancisco} incorreto.");

            await nomesBrService.InserirNovoRegistroNoRanking(nomeCristina);
            var registrosAposCristina = await nomesBrRepository.GetAll();
            var registroCristina = registrosAposCristina.FirstOrDefault(n => n.Nome == nomeCristina);
            registroFrancisco = registrosAposCristina.FirstOrDefault(n => n.Nome == nomeFrancisco);

            Assert.That(registroCristina, Is.Not.Null, $"{nomeCristina} não foi inserida.");
            Assert.That(registroFrancisco, Is.Not.Null, $"{nomeFrancisco} não encontrado após inserção de Cristina.");

            Assert.That(registroFrancisco?.Ranking, Is.EqualTo(1), $"Ranking de {nomeFrancisco} mudou incorretamente após inserir {nomeCristina}.");
            Assert.That(registroCristina?.Ranking, Is.EqualTo(2), $"Ranking de {nomeCristina} incorreto.");
            Assert.That(registrosAposCristina.Count, Is.EqualTo(2), "Número incorreto de registros após duas inserções.");

        }


        [Test]
        public async Task ExcluindoTodosOsRegistros()
        {

            await ExcluindoTodosOsRegistrosInternal();
            await nomesBrService.InserirNovoRegistroNoRanking("ParaExcluir");
            var registrosAntes = await nomesBrRepository.GetAll();
            Assert.That(registrosAntes, Is.Not.Empty, "Falha ao inserir registro de teste para exclusão.");


            await ExcluindoTodosOsRegistrosInternal();


            var registrosDepois = await nomesBrRepository.GetAll();
            Assert.That(registrosDepois, Is.Empty, "Registros não foram excluídos corretamente.");
        }


        private async Task ExcluindoTodosOsRegistrosInternal()
        {
            var registros = await nomesBrRepository.GetAll();
            foreach (var registro in registros ?? new List<Domain.Entities.NomesBr>()) 
            {
                if (registro != null)
                    await nomesBrRepository.Delete(registro.Id);
            }
        }
    }
}