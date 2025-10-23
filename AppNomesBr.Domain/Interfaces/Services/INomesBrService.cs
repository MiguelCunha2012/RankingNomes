using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using System.Threading.Tasks;

namespace AppNomesBr.Domain.Interfaces.Services
{
    public interface INomesBrService
    {
        Task<RankingNomesRoot[]> ListaTop20Nacional();
        Task<RankingNomesRoot[]> ListaTop20(string? sexo, string? codigoMunicipioIbge);
        Task<RankingNomesRoot[]> ListaTop20PorNomeMunicipio(string nomeMunicipio, string? sexo = null);
        Task<RankingNomesRoot[]> ListaMeuRanking();
        Task InserirNovoRegistroNoRanking(string nome, string? sexo = null);
    }
}