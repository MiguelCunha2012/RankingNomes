using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos
{
    public class ViaCepApi
    {
        private readonly HttpClient httpClient;

        public ViaCepApi(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<ViaCepResponse?> ConsultarCepAsync(string cep)
        {
            var url = $"https://viacep.com.br/ws/{cep}/json/";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ViaCepResponse>(content);
        }
    }

    public class ViaCepResponse
    {
        [JsonPropertyName("ibge")]
        public string Ibge { get; set; }
        [JsonPropertyName("cep")]
        public string Cep { get; set; }
        [JsonPropertyName("localidade")]
        public string Localidade { get; set; }
    }
}