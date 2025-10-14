using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Entities;
using System.Text;
using System.Text.Json.Serialization; // Adicione este using no topo do arquivo

namespace AppNomesBr.Service
{
    public class NomesBrService : INomesBrService
    {
        private readonly INomesApi ibgeNomesApiService;
        private readonly ILogger<NomesBrService> logger;
        private readonly INomesBrRepository nomesBrRepository;
        public NomesBrService(INomesApi ibgeNomesApiService, ILogger<NomesBrService> logger, INomesBrRepository nomesBrRepository)
        {
            this.ibgeNomesApiService = ibgeNomesApiService;
            this.logger = logger;
            this.nomesBrRepository = nomesBrRepository;
        }

        public async Task<RankingNomesRoot[]> ListaTop20Nacional()
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes no Brasil");
                var result = await ibgeNomesApiService.RetornaCensosNomesRanking();
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                if (rankingNomesRoot == null)
                    throw new InvalidDataException($"Metodo: \"{nameof(ListaTop20Nacional)}\" a variavel \"rankingNomesRoot\" eh nula!");
                return rankingNomesRoot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return [];
            }
        }

        public async Task<RankingNomesRoot[]> ListaTop20(string? sexo, string? codigoMunicipioIbge)
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes filtrado. Sexo: {Sexo}, Localidade: {Localidade}", sexo, codigoMunicipioIbge);
                var result = await ibgeNomesApiService.RetornaCensosNomesRanking(sexo, codigoMunicipioIbge);
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                return rankingNomesRoot ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return [];
            }
        }

        public async Task InserirNovoRegistroNoRanking(string nome, string? sexo = null)
        {
            try
            {
                logger.LogInformation("Inserir novo registro no ranking");

                var result = await ibgeNomesApiService.RetornaCensosNomesPeriodo(nome);
                var frequenciaPeriodo = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(result);

                if (frequenciaPeriodo == null || frequenciaPeriodo.Length == 0 || frequenciaPeriodo[0] == null || frequenciaPeriodo[0].Resultado == null || frequenciaPeriodo[0].Resultado?.Count == 0)
                {
                    throw new InvalidDataException("Erro ao buscar pelos dados do nome informado");
                }

                var resultado = frequenciaPeriodo[0].Resultado!;
                // Adiciona verificação para garantir que 'resultado' não é nulo
                if (resultado == null)
                {
                    throw new InvalidDataException("Resultado é nulo após verificação anterior.");
                }

                var novoRegistro = new NomesBr
                {
                    Nome = nome,
                    Periodo = FormataPeriodo(resultado),
                    Ranking = 1,
                    Frequencia = resultado.Sum(x => x.Frequencia),
                    Sexo = string.IsNullOrWhiteSpace(sexo) ? null : sexo?.Trim().ToUpperInvariant()
                };

                List<NomesBr> antigos = await nomesBrRepository.GetAll();
                antigos.Add(novoRegistro);
                await AtualizarRanking(antigos);

                novoRegistro.Ranking = antigos[^1].Ranking;

                await nomesBrRepository.Create(novoRegistro);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
            }
        }

        public async Task<RankingNomesRoot[]> ListaMeuRanking()
        {
            var consultaTodos = await nomesBrRepository.GetAll();
            ArgumentNullException.ThrowIfNull(consultaTodos);

            var retorno = new List<RankingNomesRoot>
            {
                new RankingNomesRoot { Resultado = new List<RankingNome>() }
            };

            for (int i = 0; i < consultaTodos.Count; i++)
            {
                var novo = new RankingNome
                {
                    Nome = consultaTodos[i].Nome,
                    Ranking = consultaTodos[i].Ranking,
                    Frequencia = consultaTodos[i].Frequencia
                };

                retorno[0].Resultado?.Add(novo);
            }

            retorno[0].Resultado = retorno[0].Resultado?.OrderBy(x => x.Ranking).ToList();
            return retorno.ToArray();
        }

        private static string FormataPeriodo(List<FrequenciaPeriodo>? periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            string primeiroPeriodo = periodo[0].Periodo;
            string? UltimoPeriodo = periodo[^1].Periodo;

            if (primeiroPeriodo != UltimoPeriodo)
            {
                StringBuilder sb = new();
                if (primeiroPeriodo?[..1] == "[")
                {
                    sb.Append('[');
                    primeiroPeriodo = primeiroPeriodo.Substring(1, 4);
                    sb.Append(primeiroPeriodo);
                    sb.Append(" - ");
                    string? temp = UltimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }
                else
                {
                    sb.Append('[');
                    sb.Append(primeiroPeriodo?.Replace("[", " - "));
                    string? temp = UltimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }

                return sb.ToString();
            }

            return primeiroPeriodo;
        }

        private static List<NomesBr> OrganizarRanking(List<NomesBr> nomes)
        {
            var ordenados = nomes.OrderByDescending(x => x.Frequencia).ToList();
            for (int i = 0; i < ordenados.Count; i++)
                ordenados[i].Ranking = i + 1;

            return ordenados;
        }

        private async Task AtualizarRanking(List<NomesBr> nomes)
        {
            nomes = OrganizarRanking(nomes);
            for (int i = 0; i < nomes.Count; i++)
                await nomesBrRepository.Update(nomes[i]);
        }

        // Adicione um método para buscar o IBGE pelo CEP
        public async Task<RankingNomesRoot[]> ListaTop20PorCep(string cep, string? sexo = null)
        {
            try
            {
                // 1. Consulta o ViaCEP
                using var httpClient = new HttpClient();
                var viaCepUrl = $"https://viacep.com.br/ws/{cep}/json/";
                var viaCepResponse = await httpClient.GetStringAsync(viaCepUrl);
                var viaCepData = JsonSerializer.Deserialize<ViaCepResponse>(viaCepResponse);

                if (viaCepData == null || string.IsNullOrWhiteSpace(viaCepData.Ibge))
                    throw new InvalidDataException("Não foi possível obter o código IBGE pelo CEP informado.");

                // 2. Usa o código IBGE para filtrar os nomes
                return await ListaTop20(sexo, viaCepData.Ibge);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return [];
            }
        }

        // Classe para deserializar resposta do ViaCEP
        public class ViaCepResponse
        {
            [JsonPropertyName("ibge")]
            public string Ibge { get; set; }
            // ... outros campos se necessário
        }
    }
}
