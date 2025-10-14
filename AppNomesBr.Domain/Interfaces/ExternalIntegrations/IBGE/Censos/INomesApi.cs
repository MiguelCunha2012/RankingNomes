using System.Threading.Tasks;

namespace AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos
{
    public interface INomesApi
    {
        Task<string> RetornaCensosNomesRanking();
        Task<string> RetornaCensosNomesPeriodo(string nome);
        Task<string> RetornaCensosNomesRanking(string? sexo, string? localidadeCodigoIbge);
    }
}
