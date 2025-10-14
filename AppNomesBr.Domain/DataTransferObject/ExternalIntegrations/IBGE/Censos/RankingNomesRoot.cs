using System.Text.Json.Serialization;

namespace AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos
{
    public class RankingNomesRoot
    {
        [JsonPropertyName("localidade")]
        public string Localidade { get; set; } = string.Empty;

        [JsonPropertyName("sexo")]
        public string? Sexo { get; set; }

        [JsonPropertyName("res")]
        public List<RankingNome>? Resultado { get; set; }
    }
}
