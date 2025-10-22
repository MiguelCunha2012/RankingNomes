using AppNomesBr.Domain.Interfaces.Services;
using Microsoft.Maui.Controls;
using System.Linq; // Adicionado
using System.Threading.Tasks; // Adicionado

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;
    // --- Variáveis de controle atualizadas ---
   
    // --- Fim Variáveis ---

    public RankingNomesBrasileiros(INomesBrService service)
    {
        this.service = service;
        InitializeComponent();

        // --- Obtenção das referências atualizada ---
        BtnAtualizar = this.FindByName<Button>("BtnAtualizar");
        PckSexo = this.FindByName<Picker>("PckSexo");
        TxtMunicipioNome = this.FindByName<Entry>("TxtMunicipioNome"); // Alterado
        GrdNomesBr = this.FindByName<CollectionView>("GrdNomesBr");
        // --- Fim Obtenção ---

        if (BtnAtualizar != null)
        {
            BtnAtualizar.Clicked += BtnAtualizar_Clicked;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ERRO] Botão 'BtnAtualizar' não encontrado.");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing(); // Chamar base primeiro
        await CarregarNomes(); // Carregar ranking nacional ao iniciar
    }

    // --- Método CarregarNomes modificado ---
    private async Task CarregarNomes(string? sexo = null, string? nomeMunicipio = null) // Parâmetro alterado
    {
        try
        {
            IsBusy = true; // Indica que a operação está em andamento
            GrdNomesBr.ItemsSource = null; // Limpa a lista antes de carregar

            Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNomesRoot[]? result;

            if (string.IsNullOrWhiteSpace(nomeMunicipio))
            {
                // Busca ranking nacional se nome do município estiver vazio
                result = await service.ListaTop20Nacional();
            }
            else
            {
                // Busca ranking por nome do município
                result = await service.ListaTop20PorNomeMunicipio(nomeMunicipio, sexo);
            }

            if (GrdNomesBr != null)
            {
                // Garante que o resultado e a lista interna não sejam nulos
                GrdNomesBr.ItemsSource = result?.FirstOrDefault()?.Resultado ?? new List<Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNome>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERRO] Falha ao carregar nomes: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao carregar o ranking. Verifique sua conexão ou o nome do município.", "OK");
            }
        }
        finally
        {
            IsBusy = false; // Indica que a operação terminou
        }

    }
    // --- Fim Método ---

    // --- Método BtnAtualizar_Clicked modificado ---
    private async void BtnAtualizar_Clicked(object? sender, EventArgs e)
    {
        string? sexo = PckSexo?.SelectedItem?.ToString();
        string? municipioNome = TxtMunicipioNome?.Text?.Trim(); // Obtém o nome do município

        // Chama CarregarNomes com o nome do município
        await CarregarNomes(sexo, municipioNome);
    }
    // --- Fim Método ---
}