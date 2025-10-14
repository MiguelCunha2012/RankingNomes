using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using System.Net.Http;

namespace AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos
{
    public class NomesApi : INomesApi
    {
        private readonly string? baseUrl = "api/v2/censos/nomes/";
        private readonly string rankingEndpoint = "ranking";
        private readonly HttpClient httpClient;

        public NomesApi(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.rankingEndpoint = baseUrl + this.rankingEndpoint;
        }

        public async Task<string> RetornaCensosNomesRanking()
        {
            var response = await httpClient.GetAsync(rankingEndpoint);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RetornaCensosNomesRanking(string? sexo, string? localidadeCodigoIbge)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(sexo))
                query.Add($"sexo={sexo}");
            if (!string.IsNullOrWhiteSpace(localidadeCodigoIbge))
                query.Add($"localidade={localidadeCodigoIbge}");

            var url = rankingEndpoint + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RetornaCensosNomesPeriodo(string nome)
        {
            var url = baseUrl + nome;
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
