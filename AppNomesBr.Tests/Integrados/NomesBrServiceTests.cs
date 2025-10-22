using AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Infrastructure.Repositories;
using AppNomesBr.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework; // Adicionado para os atributos [Test], [SetUp], [TearDown] e Assert
using System; // Adicionado para Uri
using System.Collections.Generic; // Adicionado para Dictionary
using System.Linq; // Adicionado para FirstOrDefault
using System.Net.Http; // Adicionado para HttpClient e IHttpClientFactory
using System.Threading.Tasks; // Adicionado para Task

namespace AppNomesBr.Tests.Integrados
{
    public class NomesBrServiceTests
    {
        private NomesApi apiIbge;
        private HttpClient httpClient;
        private NomesBrRepository nomesBrRepository;
        private NomesBrService nomesBrService;
        private Mock<IHttpClientFactory> mockHttpClientFactory; // Adicionado

        [SetUp]
        public void Setup()
        {
            httpClient = new() { BaseAddress = new Uri("https://servicodados.ibge.gov.br") };
            apiIbge = new(httpClient);

            var mockLogger = new Mock<ILogger<NomesBrService>>(); // Renomeado para mockLogger para clareza
            ILogger<NomesBrService> logger = mockLogger.Object;

            var inMemorySettings = new Dictionary<string, string> { { "DbName", "local_db_test.db3" } }; // Usando bd de teste
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            nomesBrRepository = new NomesBrRepository(configuration);

            // --- Adicionado: Mock do IHttpClientFactory ---
            mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            // --- Fim Adicionado ---

            // --- Alterado: Passando o mockHttpClientFactory.Object ---
            nomesBrService = new NomesBrService(apiIbge, logger, nomesBrRepository, mockHttpClientFactory.Object);
            // --- Fim Alterado ---

            // Opcional: Limpar banco antes de cada teste no Setup
            // ExcluindoTodosOsRegistrosInternal().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            httpClient?.Dispose(); // Adiciona verificação de nulo
        }

        [Test]
        public async Task TestandoConsultarRegistros()
        {
            // Garante que existe algo para consultar
            await ExcluindoTodosOsRegistrosInternal(); // Limpa primeiro
            await nomesBrService.InserirNovoRegistroNoRanking("TesteConsultaUnica");

            var results = await nomesBrRepository.GetAll();

            // Verifica se retornou pelo menos 1 registro
            Assert.That(results?.Count, Is.GreaterThan(0));
            Assert.That(results?.Any(r => r.Nome == "TesteConsultaUnica"), Is.True);
        }


        [Test]
        public async Task TestandoIncluirNovoCadastroERanking() // Nome do teste mais descritivo
        {
            await ExcluindoTodosOsRegistrosInternal(); // Limpa o banco antes do teste

            string nomeFrancisco = "Francisco"; // Mais frequente (esperado)
            string nomeCristina = "Cristina"; // Menos frequente (esperado)

            await nomesBrService.InserirNovoRegistroNoRanking(nomeFrancisco);
            var registrosAposFrancisco = await nomesBrRepository.GetAll();
            var registroFrancisco = registrosAposFrancisco.FirstOrDefault(n => n.Nome == nomeFrancisco);
            Assert.That(registroFrancisco, Is.Not.Null, $"{nomeFrancisco} não foi inserido.");
            Assert.That(registroFrancisco?.Ranking, Is.EqualTo(1), $"Ranking inicial de {nomeFrancisco} incorreto.");

            await nomesBrService.InserirNovoRegistroNoRanking(nomeCristina);
            var registrosAposCristina = await nomesBrRepository.GetAll();
            var registroCristina = registrosAposCristina.FirstOrDefault(n => n.Nome == nomeCristina);
            // Rebusca Francisco para verificar se o ranking foi atualizado corretamente após a segunda inserção
            registroFrancisco = registrosAposCristina.FirstOrDefault(n => n.Nome == nomeFrancisco);

            Assert.That(registroCristina, Is.Not.Null, $"{nomeCristina} não foi inserida.");
            Assert.That(registroFrancisco, Is.Not.Null, $"{nomeFrancisco} não encontrado após inserção de Cristina.");

            // Assume que Francisco é mais frequente que Cristina baseado nos dados do IBGE
            Assert.That(registroFrancisco?.Ranking, Is.EqualTo(1), $"Ranking de {nomeFrancisco} mudou incorretamente após inserir {nomeCristina}.");
            Assert.That(registroCristina?.Ranking, Is.EqualTo(2), $"Ranking de {nomeCristina} incorreto.");
            Assert.That(registrosAposCristina.Count, Is.EqualTo(2), "Número incorreto de registros após duas inserções.");


            // Pode continuar adicionando mais nomes e asserts aqui...
            // string nomeRodolfo = "Rodolfo";
            // await nomesBrService.InserirNovoRegistroNoRanking(nomeRodolfo);
            // var registrosAposRodolfo = await nomesBrRepository.GetAll();
            // ... verificar rankings ...
        }


        [Test]
        public async Task ExcluindoTodosOsRegistros()
        {
            // Garante que há algo para excluir
            await ExcluindoTodosOsRegistrosInternal(); // Limpa
            await nomesBrService.InserirNovoRegistroNoRanking("ParaExcluir");
            var registrosAntes = await nomesBrRepository.GetAll();
            Assert.That(registrosAntes, Is.Not.Empty, "Falha ao inserir registro de teste para exclusão.");


            // Executa a exclusão (usando o método interno)
            await ExcluindoTodosOsRegistrosInternal();


            var registrosDepois = await nomesBrRepository.GetAll();
            Assert.That(registrosDepois, Is.Empty, "Registros não foram excluídos corretamente.");
        }

        // Método auxiliar para limpar o banco (usado internamente pelos testes)
        private async Task ExcluindoTodosOsRegistrosInternal()
        {
            var registros = await nomesBrRepository.GetAll();
            foreach (var registro in registros ?? new List<Domain.Entities.NomesBr>()) // Evita erro se GetAll retornar null
            {
                if (registro != null)
                    await nomesBrRepository.Delete(registro.Id);
            }
        }
    }
}