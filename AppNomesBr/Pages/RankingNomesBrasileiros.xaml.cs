using AppNomesBr.Domain.Interfaces.Services;
using Microsoft.Maui.Controls;
using System.Linq; // Adicionado
using System.Threading.Tasks; // Adicionado

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;
    // --- Vari�veis de controle atualizadas ---
   
    // --- Fim Vari�veis ---

    public RankingNomesBrasileiros(INomesBrService service)
    {
        this.service = service;
        InitializeComponent();

        // --- Obten��o das refer�ncias atualizada ---
        BtnAtualizar = this.FindByName<Button>("BtnAtualizar");
        PckSexo = this.FindByName<Picker>("PckSexo");
        TxtMunicipioNome = this.FindByName<Entry>("TxtMunicipioNome"); // Alterado
        GrdNomesBr = this.FindByName<CollectionView>("GrdNomesBr");
        // --- Fim Obten��o ---

        if (BtnAtualizar != null)
        {
            BtnAtualizar.Clicked += BtnAtualizar_Clicked;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ERRO] Bot�o 'BtnAtualizar' n�o encontrado.");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing(); // Chamar base primeiro
        await CarregarNomes(); // Carregar ranking nacional ao iniciar
    }

    // --- M�todo CarregarNomes modificado ---
    private async Task CarregarNomes(string? sexo = null, string? nomeMunicipio = null) // Par�metro alterado
    {
        try
        {
            IsBusy = true; // Indica que a opera��o est� em andamento
            GrdNomesBr.ItemsSource = null; // Limpa a lista antes de carregar

            Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNomesRoot[]? result;

            if (string.IsNullOrWhiteSpace(nomeMunicipio))
            {
                // Busca ranking nacional se nome do munic�pio estiver vazio
                result = await service.ListaTop20Nacional();
            }
            else
            {
                // Busca ranking por nome do munic�pio
                result = await service.ListaTop20PorNomeMunicipio(nomeMunicipio, sexo);
            }

            if (GrdNomesBr != null)
            {
                // Garante que o resultado e a lista interna n�o sejam nulos
                GrdNomesBr.ItemsSource = result?.FirstOrDefault()?.Resultado ?? new List<Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNome>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERRO] Falha ao carregar nomes: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao carregar o ranking. Verifique sua conex�o ou o nome do munic�pio.", "OK");
            }
        }
        finally
        {
            IsBusy = false; // Indica que a opera��o terminou
        }

    }
    // --- Fim M�todo ---

    // --- M�todo BtnAtualizar_Clicked modificado ---
    private async void BtnAtualizar_Clicked(object? sender, EventArgs e)
    {
        string? sexo = PckSexo?.SelectedItem?.ToString();
        string? municipioNome = TxtMunicipioNome?.Text?.Trim(); // Obt�m o nome do munic�pio

        // Chama CarregarNomes com o nome do munic�pio
        await CarregarNomes(sexo, municipioNome);
    }
    // --- Fim M�todo ---
}