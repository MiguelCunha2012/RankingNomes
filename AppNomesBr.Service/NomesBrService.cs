using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Entities;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Http; // Adicionado
using System.Linq;      // Adicionado
using Microsoft.Maui.ApplicationModel; // Adicionado para MainThread

namespace AppNomesBr.Service
{
    public class NomesBrService : INomesBrService
    {
        private readonly INomesApi ibgeNomesApiService;
        private readonly ILogger<NomesBrService> logger;
        private readonly INomesBrRepository nomesBrRepository;
        private readonly IHttpClientFactory httpClientFactory; // Adicionado

        // Construtor modificado
        public NomesBrService(INomesApi ibgeNomesApiService, ILogger<NomesBrService> logger, INomesBrRepository nomesBrRepository, IHttpClientFactory httpClientFactory)
        {
            this.ibgeNomesApiService = ibgeNomesApiService;
            this.logger = logger;
            this.nomesBrRepository = nomesBrRepository;
            this.httpClientFactory = httpClientFactory; // Adicionado
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

                // Re-busca a lista ordenada para pegar o ranking correto após a atualização
                var listaAtualizada = await nomesBrRepository.GetAll();
                var registroInserido = listaAtualizada.FirstOrDefault(r => r.Nome == novoRegistro.Nome); // Encontra o registro recém-inserido/atualizado
                novoRegistro.Ranking = registroInserido?.Ranking ?? antigos.Count; // Atualiza o ranking do objeto antes de criar/atualizar

                // Verifica se o nome já existe para decidir entre Create e Update
                var registroExistente = antigos.FirstOrDefault(r => r.Nome.Equals(novoRegistro.Nome, StringComparison.OrdinalIgnoreCase));
                if (registroExistente != null)
                {
                    novoRegistro.Id = registroExistente.Id; // Garante que o ID correto seja usado para Update
                    await nomesBrRepository.Update(novoRegistro);
                    logger.LogInformation("Registro atualizado para o nome: {Nome}", nome);
                }
                else
                {
                    await nomesBrRepository.Create(novoRegistro);
                    logger.LogInformation("Novo registro criado para o nome: {Nome}", nome);
                }

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

            // Ordena aqui antes de criar o DTO de retorno
            var consultaOrdenada = consultaTodos.OrderBy(x => x.Ranking).ToList();

            for (int i = 0; i < consultaOrdenada.Count; i++)
            {
                var novo = new RankingNome
                {
                    Nome = consultaOrdenada[i].Nome,
                    Ranking = consultaOrdenada[i].Ranking,
                    Frequencia = consultaOrdenada[i].Frequencia
                    // Se quiser exibir o sexo no "Meu Ranking", adicione:
                    // Sexo = consultaTodos[i].Sexo
                };

                retorno[0].Resultado?.Add(novo);
            }

            // A ordenação já foi feita antes do loop
            // retorno[0].Resultado = retorno[0].Resultado?.OrderBy(x => x.Ranking).ToList();
            return retorno.ToArray();
        }

        private static string FormataPeriodo(List<FrequenciaPeriodo>? periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            // Garante que a lista não está vazia antes de acessar índices
            if (periodo.Count == 0) return string.Empty;


            string primeiroPeriodo = periodo[0].Periodo;
            string? ultimoPeriodo = periodo[^1].Periodo; // ^1 pega o último elemento

            if (primeiroPeriodo == ultimoPeriodo || string.IsNullOrEmpty(ultimoPeriodo))
            {
                // Retorna apenas o primeiro período se for o único ou se o último for inválido
                return primeiroPeriodo ?? string.Empty;
            }

            StringBuilder sb = new();
            // Extrai o ano inicial do primeiro período
            string anoInicial = primeiroPeriodo.TrimStart('[').Split(',')[0].Trim();
            // Extrai o ano final do último período
            string anoFinal = ultimoPeriodo.Split(',')[1].TrimEnd(']').Trim();

            sb.Append('[');
            sb.Append(anoInicial);
            sb.Append(" - ");
            sb.Append(anoFinal);
            sb.Append(']');

            return sb.ToString();

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
            // Reorganiza o ranking baseado na frequência atual
            var nomesOrdenados = OrganizarRanking(new List<NomesBr>(nomes)); // Cria cópia para não modificar a original inesperadamente

            // Atualiza cada registro no banco de dados com seu novo ranking
            foreach (var nomeParaAtualizar in nomesOrdenados)
            {
                // Busca o registro existente no banco pelo nome (ou ID se já tiver)
                // É importante garantir que você está atualizando o registro correto
                var registroExistente = await nomesBrRepository.GetAll().ContinueWith(t => t.Result.FirstOrDefault(n => n.Nome.Equals(nomeParaAtualizar.Nome, StringComparison.OrdinalIgnoreCase)));

                if (registroExistente != null)
                {
                    registroExistente.Ranking = nomeParaAtualizar.Ranking; // Atualiza apenas o ranking
                                                                           // Opcional: Atualizar outros campos se necessário, como Frequencia se ela puder mudar
                                                                           // registroExistente.Frequencia = nomeParaAtualizar.Frequencia;
                    await nomesBrRepository.Update(registroExistente);
                }
                // else: O registro pode ter sido removido ou é o novo registro ainda não salvo.
                // O novo registro será salvo com o ranking correto após esta chamada.
            }
        }


        // --- Adicionado ---
        // Método auxiliar para buscar código IBGE pelo nome do município
        private async Task<string?> GetCodigoIbgePorNomeMunicipio(string nomeMunicipio)
        {
            try
            {
                logger.LogInformation("Consultando código IBGE para o município: {NomeMunicipio}", nomeMunicipio);
                var client = httpClientFactory.CreateClient();
                var url = "https://servicodados.ibge.gov.br/api/v1/localidades/municipios"; // API de Localidades
                var response = await client.GetStringAsync(url);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var municipios = JsonSerializer.Deserialize<List<MunicipioIbgeResponse>>(response, options);

                var nomeMunicipioNormalizado = nomeMunicipio.Trim().ToUpperInvariant();

                var municipioEncontrado = municipios?
                    .FirstOrDefault(m => m.Nome?.Trim().ToUpperInvariant() == nomeMunicipioNormalizado);

                if (municipioEncontrado == null)
                {
                    // Tentativa de remover acentos para encontrar correspondência
                    var nomeSemAcento = RemoverAcentos(nomeMunicipioNormalizado);
                    municipioEncontrado = municipios?
                        .FirstOrDefault(m => RemoverAcentos(m.Nome?.Trim().ToUpperInvariant()) == nomeSemAcento);
                }


                if (municipioEncontrado == null)
                {
                    logger.LogWarning("Município '{NomeMunicipio}' não encontrado na API de Localidades do IBGE.", nomeMunicipio);
                    return null;
                }

                return municipioEncontrado.Id?.ToString(); // O ID é o código IBGE
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogError(httpEx, "[ERRO HTTP {StatusCode}] ao buscar código IBGE para {NomeMunicipio}: Endpoint não encontrado ou município inválido.", httpEx.StatusCode, nomeMunicipio);
                return null; // Retorna null ou lança uma exceção específica
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO] ao buscar código IBGE para {NomeMunicipio}: {Message}", nomeMunicipio, ex.Message);
                return null;
            }
        }
        // Função auxiliar para remover acentos (simples)
        private static string RemoverAcentos(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            var sb = new StringBuilder();
            var arrayTexto = texto.Normalize(NormalizationForm.FormD).ToCharArray();

            foreach (char letra in arrayTexto)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(letra) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(letra);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }


        // Implementação do novo método da interface
        public async Task<RankingNomesRoot[]> ListaTop20PorNomeMunicipio(string nomeMunicipio, string? sexo = null)
        {
            if (string.IsNullOrWhiteSpace(nomeMunicipio))
            {
                logger.LogWarning("Nome do município não fornecido para ListaTop20PorNomeMunicipio.");
                // Se o nome estiver vazio, talvez retornar o ranking nacional? Ou vazio?
                // return await ListaTop20Nacional(); // Opção 1: Retorna nacional
                return []; // Opção 2: Retorna vazio
            }

            string? codigoIbge = await GetCodigoIbgePorNomeMunicipio(nomeMunicipio);

            if (string.IsNullOrWhiteSpace(codigoIbge))
            {
                logger.LogError("Não foi possível encontrar o código IBGE para o município: {NomeMunicipio}", nomeMunicipio);
                // Informa o usuário que o município não foi encontrado
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Erro", $"Município '{nomeMunicipio}' não encontrado.", "OK");
                    }
                });
                return []; // Retorna um array vazio para indicar que nada foi encontrado
            }

            // Chama o método existente que usa o código IBGE
            return await ListaTop20(sexo, codigoIbge);
        }
        // --- Fim Adicionado ---


        // Classe para deserializar resposta do ViaCEP (Manter se usar ListaTop20PorCep)
        public class ViaCepResponse
        {
            [JsonPropertyName("ibge")]
            public string Ibge { get; set; } = string.Empty; // Inicializar para evitar null
            // ... outros campos se necessário
        }

        // --- Adicionado ---
        // Classe auxiliar para deserializar a resposta da API de Localidades do IBGE
        private class MunicipioIbgeResponse
        {
            [JsonPropertyName("id")]
            public long? Id { get; set; } // O ID é o código IBGE

            [JsonPropertyName("nome")]
            public string? Nome { get; set; }
        }
        // --- Fim Adicionado ---
    }
}