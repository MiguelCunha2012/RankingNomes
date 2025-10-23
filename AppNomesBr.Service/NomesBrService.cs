using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Entities;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Http; 
using System.Linq;     
using Microsoft.Maui.ApplicationModel; 
using System; 
using System.Collections.Generic; 
using System.Threading.Tasks; 
using System.Globalization; 

namespace AppNomesBr.Service
{
    public class NomesBrService : INomesBrService
    {
        private readonly INomesApi ibgeNomesApiService;
        private readonly ILogger<NomesBrService> logger;
        private readonly INomesBrRepository nomesBrRepository;
        private readonly IHttpClientFactory httpClientFactory;

        public NomesBrService(INomesApi ibgeNomesApiService, ILogger<NomesBrService> logger, INomesBrRepository nomesBrRepository, IHttpClientFactory httpClientFactory)
        {
            this.ibgeNomesApiService = ibgeNomesApiService;
            this.logger = logger;
            this.nomesBrRepository = nomesBrRepository;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<RankingNomesRoot[]> ListaTop20Nacional()
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes no Brasil");
                var result = await ibgeNomesApiService.RetornaCensosNomesRanking();
                if (string.IsNullOrWhiteSpace(result) || !result.TrimStart().StartsWith("["))
                {
                    logger.LogWarning("API RetornaCensosNomesRanking retornou resultado vazio ou inválido.");
                    return [];
                }
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                if (rankingNomesRoot == null)
                {
                    logger.LogError("Deserialização de ListaTop20Nacional resultou em nulo.");
                    return [];
                }

                return rankingNomesRoot;
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "[ERRO JSON] Falha ao deserializar ListaTop20Nacional: {Message}", jsonEx.Message);
                return [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO GERAL] em ListaTop20Nacional: {Message}", ex.Message);
                return [];
            }
        }

        public async Task<RankingNomesRoot[]> ListaTop20(string? sexo, string? codigoMunicipioIbge)
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes filtrado. Sexo: {Sexo}, Localidade: {Localidade}", sexo ?? "N/A", codigoMunicipioIbge ?? "N/A");
                var result = await ibgeNomesApiService.RetornaCensosNomesRanking(sexo, codigoMunicipioIbge);
                
                if (string.IsNullOrWhiteSpace(result) || !result.TrimStart().StartsWith("["))
                {
                    logger.LogWarning("API RetornaCensosNomesRanking (filtrado) retornou resultado vazio ou inválido para Sexo={Sexo}, Localidade={Localidade}.", sexo ?? "N/A", codigoMunicipioIbge ?? "N/A");
                    return [];
                }
                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                return rankingNomesRoot ?? []; 
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "[ERRO JSON] Falha ao deserializar ListaTop20 (filtrado) para Sexo={Sexo}, Localidade={Localidade}: {Message}", sexo ?? "N/A", codigoMunicipioIbge ?? "N/A", jsonEx.Message);
                return [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO GERAL] em ListaTop20 (filtrado) para Sexo={Sexo}, Localidade={Localidade}: {Message}", sexo ?? "N/A", codigoMunicipioIbge ?? "N/A", ex.Message);
                return [];
            }
        }

        public async Task InserirNovoRegistroNoRanking(string nome, string? sexo = null)
        {
            var nomeNormalizado = nome?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(nomeNormalizado))
            {
                logger.LogWarning("Nome inválido fornecido para InserirNovoRegistroNoRanking.");
                return;
            }

            try
            {
                logger.LogInformation("Iniciando processo para inserir/atualizar nome: {Nome}, Sexo: {Sexo}", nomeNormalizado, sexo ?? "N/A");

                var resultApi = await ibgeNomesApiService.RetornaCensosNomesPeriodo(nomeNormalizado);
                if (string.IsNullOrWhiteSpace(resultApi) || !resultApi.TrimStart().StartsWith("["))
                {
                    logger.LogWarning("API RetornaCensosNomesPeriodo retornou resultado vazio ou inválido para o nome: {Nome}", nomeNormalizado);
                    MainThread.BeginInvokeOnMainThread(async () => {
                        if (Application.Current?.MainPage != null)
                            await Application.Current.MainPage.DisplayAlert("Aviso", $"Não foram encontrados dados de frequência para '{nomeNormalizado}' na base do IBGE.", "OK");
                    });
                    return;
                }

                NomeFrequenciaPeriodoRoot[]? frequenciaPeriodo = null;
                try
                {
                    frequenciaPeriodo = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(resultApi);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "[ERRO JSON] Falha ao deserializar RetornaCensosNomesPeriodo para {Nome}: {Message}", nomeNormalizado, jsonEx.Message);
                    throw new InvalidDataException($"Falha ao processar dados da API do IBGE para o nome: {nomeNormalizado}", jsonEx); // Re-lança para o catch externo
                }


                if (frequenciaPeriodo == null || frequenciaPeriodo.Length == 0 || frequenciaPeriodo[0]?.Resultado == null || !frequenciaPeriodo[0].Resultado.Any())
                {
                    logger.LogWarning("Não foram encontrados dados de frequência na API do IBGE para o nome: {Nome}", nomeNormalizado);
                    MainThread.BeginInvokeOnMainThread(async () => {
                        if (Application.Current?.MainPage != null)
                            await Application.Current.MainPage.DisplayAlert("Aviso", $"Não foram encontrados dados de frequência para '{nomeNormalizado}' na base do IBGE.", "OK");
                    });
                    return;
                }

                var resultadoApi = frequenciaPeriodo[0].Resultado!;
                long frequenciaTotal = resultadoApi.Sum(x => x.Frequencia);
                string periodoFormatado = FormataPeriodo(resultadoApi);
                string? sexoFormatado = string.IsNullOrWhiteSpace(sexo) ? null : sexo.Trim().ToUpperInvariant();

                var todosRegistrosAntes = await nomesBrRepository.GetAll();
                var registroExistente = todosRegistrosAntes?.FirstOrDefault(r => r.Nome.Equals(nomeNormalizado, StringComparison.OrdinalIgnoreCase)); // Adiciona verificação de nulo

                if (registroExistente != null)
                {
                    logger.LogInformation("Nome '{Nome}' já existe. Atualizando frequência ({Frequencia}), período ({Periodo}) e sexo ({Sexo}).", nomeNormalizado, frequenciaTotal, periodoFormatado, sexoFormatado ?? "N/A");
                    registroExistente.Frequencia = frequenciaTotal;
                    registroExistente.Periodo = periodoFormatado;
                    registroExistente.Sexo = sexoFormatado;
                    await nomesBrRepository.Update(registroExistente);
                }
                else
                {
                    logger.LogInformation("Nome '{Nome}' não existe. Criando novo registro com Frequencia={Frequencia}, Periodo={Periodo}, Sexo={Sexo}.", nomeNormalizado, frequenciaTotal, periodoFormatado, sexoFormatado ?? "N/A");
                    var novoRegistro = new NomesBr
                    {
                        Nome = nomeNormalizado,
                        Periodo = periodoFormatado,
                        Frequencia = frequenciaTotal,
                        Sexo = sexoFormatado,
                        Ranking = 0 
                    };
                    await nomesBrRepository.Create(novoRegistro);
                }

                await RecalcularEAtualizarTodosOsRankings();
                logger.LogInformation("Processo para {Nome} concluído.", nomeNormalizado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO GERAL] Falha ao inserir/atualizar registro no ranking para {Nome}: {Message}", nomeNormalizado, ex.Message);
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (Application.Current?.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert("Erro", $"Ocorreu um erro ao processar o nome '{nomeNormalizado}'. Verifique os logs.", "OK");
                });
            }
        }
        

        public async Task<RankingNomesRoot[]> ListaMeuRanking()
        {
            var consultaTodos = await nomesBrRepository.GetAll();
            if (consultaTodos == null)
            {
                logger.LogWarning("GetAll retornou nulo ao buscar Meu Ranking.");
                return [];
            }


            var retorno = new List<RankingNomesRoot>
            {
                new RankingNomesRoot { Resultado = new List<RankingNome>() }
            };

            var consultaOrdenada = consultaTodos.OrderBy(x => x.Ranking).ThenBy(x => x.Nome).ToList(); 

            for (int i = 0; i < consultaOrdenada.Count; i++)
            {
                if (consultaOrdenada[i] == null) continue; 

                var novo = new RankingNome
                {
                    Nome = consultaOrdenada[i].Nome,
                    Ranking = consultaOrdenada[i].Ranking,
                    Frequencia = consultaOrdenada[i].Frequencia,
                    Sexo = consultaOrdenada[i].Sexo
                };

                retorno[0].Resultado?.Add(novo); 
            }

            return retorno.ToArray();
        }

        private static string FormataPeriodo(List<FrequenciaPeriodo>? periodo)
        {
           
            if (periodo == null || periodo.Count == 0) return string.Empty;

          
            var periodosValidos = periodo.Where(p => !string.IsNullOrWhiteSpace(p?.Periodo) && p.Periodo.Contains("[") && p.Periodo.Contains(",") && p.Periodo.Contains("]")).ToList();
            if (periodosValidos.Count == 0) return string.Empty;


            string primeiroPeriodoStr = periodosValidos[0].Periodo;
            string ultimoPeriodoStr = periodosValidos[^1].Periodo;

            if (primeiroPeriodoStr == ultimoPeriodoStr)
            {
                
                try
                {
                    string anoUnicoInicial = primeiroPeriodoStr.TrimStart('[').Split(',')[0].Trim();
                    string anoUnicoFinal = primeiroPeriodoStr.Split(',')[1].TrimEnd(']').Trim();
                    
                    if (int.TryParse(anoUnicoInicial, out _) && int.TryParse(anoUnicoFinal, out _))
                    {
                        return $"[{anoUnicoInicial} - {anoUnicoFinal}]";
                    }
                }
                catch { }
                return primeiroPeriodoStr; 
            }

            try
            {
                StringBuilder sb = new();
                string anoInicial = primeiroPeriodoStr.TrimStart('[').Split(',')[0].Trim();
                string anoFinal = ultimoPeriodoStr.Split(',')[1].TrimEnd(']').Trim();

        
                if (!int.TryParse(anoInicial, out _) || !int.TryParse(anoFinal, out _))
                {
                    return $"{primeiroPeriodoStr} a {ultimoPeriodoStr}"; 
                }


                sb.Append('[');
                sb.Append(anoInicial);
                sb.Append(" - ");
                sb.Append(anoFinal);
                sb.Append(']');
                return sb.ToString();
            }
            catch (Exception ex) 
            {
                
                return $"{primeiroPeriodoStr} a {ultimoPeriodoStr}";
            }
        }


        
        private static List<NomesBr> OrganizarRanking(List<NomesBr> nomes)
        {
          
            if (nomes == null || !nomes.Any()) return new List<NomesBr>();

           
            var ordenados = nomes
                .OrderByDescending(x => x.Frequencia)
                .ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase) 
                .ToList();

            for (int i = 0; i < ordenados.Count; i++)
                ordenados[i].Ranking = i + 1; 

            return ordenados;
        }

    
        private async Task RecalcularEAtualizarTodosOsRankings()
        {
            logger.LogInformation("Iniciando RecalcularEAtualizarTodosOsRankings...");
            try
            {
                var todosRegistros = await nomesBrRepository.GetAll();
                if (todosRegistros == null || !todosRegistros.Any())
                {
                    logger.LogInformation("Nenhum registro encontrado para atualizar rankings.");
                    return;
                }
                logger.LogDebug("Recalculando rankings para {Count} registros.", todosRegistros.Count);

                
                var registrosComNovoRanking = OrganizarRanking(todosRegistros);

                logger.LogInformation("Atualizando rankings no banco de dados...");
                int updatedCount = 0;
                int errorCount = 0; 

                foreach (var registroCalculado in registrosComNovoRanking)
                {
                    if (registroCalculado.Id > 0)
                    {
                      
                        logger.LogDebug("Processando ID {Id} (Nome: {Nome}). Ranking calculado: {CalculadoRanking}",
                                         registroCalculado.Id, registroCalculado.Nome, registroCalculado.Ranking);

                        NomesBr? registroDoBanco = null;
                        try
                        {
                            registroDoBanco = await nomesBrRepository.GetById(registroCalculado.Id);

                            if (registroDoBanco != null)
                            {
                              
                                logger.LogDebug("  Registro encontrado no banco. Ranking atual no banco: {BancoRanking}. Tentando atualizar para: {CalculadoRanking}",
                                                 registroDoBanco.Ranking, registroCalculado.Ranking);

                                if (registroDoBanco.Ranking != registroCalculado.Ranking)
                                {
                                    registroDoBanco.Ranking = registroCalculado.Ranking;
                                    await nomesBrRepository.Update(registroDoBanco); 
                                    updatedCount++;
                                 
                                    logger.LogDebug("  Update chamado para ID {Id}. Ranking agora definido como {NovoRanking}.",
                                                    registroDoBanco.Id, registroDoBanco.Ranking);

                                }
                                else
                                {
                                    logger.LogDebug("  Ranking já está correto ({Ranking}). Nenhuma atualização necessária.", registroDoBanco.Ranking);
                                }
                            }
                            else
                            {
                                logger.LogWarning("  Registro com ID {Id} (Nome: {Nome}) não encontrado no banco via GetById durante atualização de ranking.",
                                                   registroCalculado.Id, registroCalculado.Nome);
                                errorCount++;
                            }
                        }
                        catch (Exception updateEx) // Captura erro específico da atualização
                        {
                            logger.LogError(updateEx, "  [ERRO no UPDATE] Falha ao atualizar ranking para ID {Id} (Nome: {Nome}).",
                                            registroCalculado.Id, registroCalculado.Nome);
                            errorCount++;
                        }
                    }
                    else
                    {
                        logger.LogWarning("Tentativa de atualizar ranking para registro sem ID (Nome: {Nome}).", registroCalculado.Nome);
                        errorCount++;
                    }
                }
                logger.LogInformation("Atualização de rankings no banco concluída. {UpdatedCount} registros modificados. {ErrorCount} erros.", updatedCount, errorCount);
            }
            catch (Exception ex)
            {
               
                logger.LogError(ex, "[ERRO CRÍTICO] Falha durante RecalcularEAtualizarTodosOsRankings: {Message}\nStackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }
            logger.LogInformation("Finalizando RecalcularEAtualizarTodosOsRankings.");
        }
       


        private async Task<string?> GetCodigoIbgePorNomeMunicipio(string nomeMunicipio)
        {
            try
            {
                logger.LogInformation("Consultando código IBGE para o município: {NomeMunicipio}", nomeMunicipio);
                var client = httpClientFactory.CreateClient("IBGELocalidades"); 
                var url = "https://servicodados.ibge.gov.br/api/v1/localidades/municipios";
                var response = await client.GetStringAsync(url);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<MunicipioIbgeResponse>? municipios = null;
                try
                {
                    municipios = JsonSerializer.Deserialize<List<MunicipioIbgeResponse>>(response, options);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "[ERRO JSON] Falha ao deserializar lista de municípios: {Message}", jsonEx.Message);
                    return null;
                }


                if (municipios == null) return null;

                var nomeMunicipioNormalizado = nomeMunicipio.Trim().ToUpperInvariant();
                var nomeSemAcento = RemoverAcentos(nomeMunicipioNormalizado);

                
                var municipioEncontrado = municipios
                    .FirstOrDefault(m => m.Nome?.Trim().ToUpperInvariant() == nomeMunicipioNormalizado);

               
                if (municipioEncontrado == null)
                {
                    municipioEncontrado = municipios
                        .FirstOrDefault(m => RemoverAcentos(m.Nome?.Trim().ToUpperInvariant()) == nomeSemAcento);
                }

                if (municipioEncontrado == null)
                {
                    logger.LogWarning("Município '{NomeMunicipio}' não encontrado na API de Localidades do IBGE.", nomeMunicipio);
                    return null;
                }

                logger.LogInformation("Código IBGE encontrado para '{NomeMunicipio}': {CodigoIbge}", nomeMunicipio, municipioEncontrado.Id);
                return municipioEncontrado.Id?.ToString();
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogError(httpEx, "[ERRO HTTP {StatusCode}] ao buscar código IBGE para {NomeMunicipio}: Endpoint não encontrado.", httpEx.StatusCode, nomeMunicipio);
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                logger.LogError(httpEx, "[ERRO HTTP {StatusCode}] ao buscar código IBGE para {NomeMunicipio}: {Message}", httpEx.StatusCode ?? System.Net.HttpStatusCode.InternalServerError, nomeMunicipio, httpEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO GERAL] ao buscar código IBGE para {NomeMunicipio}: {Message}", nomeMunicipio, ex.Message);
                return null;
            }
        }

        private static string RemoverAcentos(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            try
            {
                var sb = new StringBuilder();
               
                var arrayTexto = texto.Normalize(NormalizationForm.FormD).ToCharArray();

                foreach (char letra in arrayTexto)
                {
                   
                    if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                    {
                        sb.Append(letra);
                    }
                }
                
                return sb.ToString().Normalize(NormalizationForm.FormC);
            }
            catch (Exception ex) 
            {
               
                return texto; 
            }
        }


        public async Task<RankingNomesRoot[]> ListaTop20PorNomeMunicipio(string nomeMunicipio, string? sexo = null)
        {
            if (string.IsNullOrWhiteSpace(nomeMunicipio))
            {
                logger.LogWarning("Nome do município não fornecido para ListaTop20PorNomeMunicipio.");
                return [];
            }

            string? codigoIbge = await GetCodigoIbgePorNomeMunicipio(nomeMunicipio);

            if (string.IsNullOrWhiteSpace(codigoIbge))
            {
                logger.LogError("Não foi possível encontrar o código IBGE para o município: {NomeMunicipio}", nomeMunicipio);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Erro", $"Município '{nomeMunicipio}' não encontrado.", "OK");
                    }
                });
                return [];
            }

            return await ListaTop20(sexo, codigoIbge);
        }


        
        public class ViaCepResponse
        {
            [JsonPropertyName("ibge")]
            public string Ibge { get; set; } = string.Empty;
        }

        private class MunicipioIbgeResponse
        {
            [JsonPropertyName("id")]
            public long? Id { get; set; }

            [JsonPropertyName("nome")]
            public string? Nome { get; set; }
        }
    }
}