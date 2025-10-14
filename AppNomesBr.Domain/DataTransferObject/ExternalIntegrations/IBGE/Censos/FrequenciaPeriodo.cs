using System.Text.Json.Serialization;

namespace AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos
{
    public class FrequenciaPeriodo
    {
        [JsonPropertyName("periodo")]
        public string Periodo { get; set; } = string.Empty;

        [JsonPropertyName("frequencia")]
        public long Frequencia { get; set; }
    }
}
