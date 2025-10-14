namespace AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos
{
    public class RankingNomeDto
    {
        public string Nome { get; set; } = string.Empty;
        public long Frequencia { get; set; }
        public int Ranking { get; set; }
        public string? Sexo { get; set; } 
    }
}