using AppNomesBr.Domain.Interfaces.Services;
using Microsoft.Maui.Controls;
using System.Linq; 
using System.Threading.Tasks; 

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;

    public RankingNomesBrasileiros(INomesBrService service)
    {
        this.service = service;
        InitializeComponent();


        BtnAtualizar = this.FindByName<Button>("BtnAtualizar");
        PckSexo = this.FindByName<Picker>("PckSexo");
        TxtMunicipioNome = this.FindByName<Entry>("TxtMunicipioNome"); 
        GrdNomesBr = this.FindByName<CollectionView>("GrdNomesBr");


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
        base.OnAppearing();
        await CarregarNomes();
    }


    private async Task CarregarNomes(string? sexo = null, string? nomeMunicipio = null) 
    {
        try
        {
            IsBusy = true; 
            GrdNomesBr.ItemsSource = null; 

            Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNomesRoot[]? result;

            if (string.IsNullOrWhiteSpace(nomeMunicipio))
            {

                result = await service.ListaTop20Nacional();
            }
            else
            {

                result = await service.ListaTop20PorNomeMunicipio(nomeMunicipio, sexo);
            }

            if (GrdNomesBr != null)
            {

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
            IsBusy = false;
        }

    }



    private async void BtnAtualizar_Clicked(object? sender, EventArgs e)
    {
        string? sexo = PckSexo?.SelectedItem?.ToString();
        string? municipioNome = TxtMunicipioNome?.Text?.Trim();

        await CarregarNomes(sexo, municipioNome);
    }
}