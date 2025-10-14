using System.Text.Json.Serialization;

namespace AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos
{
    public class NomeFrequenciaPeriodoRoot
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("sexo")]
        public string? Sexo { get; set; }

        [JsonPropertyName("localidade")]
        public string Localidade { get; set; } = string.Empty;

        [JsonPropertyName("res")]
        public List<FrequenciaPeriodo>? Resultado { get; set; }
    }
}
