using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Entities;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AppNomesBr.Tests.Unitarios
{
    public class ApiIbgeServiceTest
    {
        private NomesApi apiIbge;
        private HttpClient httpClient;

        [SetUp]
        public void Setup()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://servicodados.ibge.gov.br") };
            apiIbge = new(httpClient);
        }

        [TearDown]
        public void TearDown()
        {
            httpClient.Dispose();
        }

        [Test]
        public async Task TestandoRetornoApi()
        {
            var rankingNomesRootResult = await apiIbge.RetornaCensosNomesRanking();
            var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(rankingNomesRootResult);
            var nomeFrequenciaPeriodoRootResult = await apiIbge.RetornaCensosNomesPeriodo("Pedro|Francisco");
            var nomeFrequenciaPeriodoRoot = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(nomeFrequenciaPeriodoRootResult);

            Assert.Multiple(() =>
            {
                Assert.That(rankingNomesRoot?.Length, Is.GreaterThan(0));
                Assert.That(nomeFrequenciaPeriodoRoot?.Length, Is.GreaterThan(0));
            });
        }

        [Test]
        public async Task TestandoConsultaErradaNaApiNomesPeriodo()
        {
            var nomeFrequenciaPeriodoRootResult = await apiIbge.RetornaCensosNomesPeriodo("NomeInexistente");
            var nomeFrequenciaPeriodoRoot = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(nomeFrequenciaPeriodoRootResult);

            Assert.That(nomeFrequenciaPeriodoRoot?.Length, Is.EqualTo(0));
        }
    }
}