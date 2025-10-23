using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Interfaces.Services;
using System.Linq; 
using System.Threading.Tasks; 
using Microsoft.Maui.Controls; 
using Microsoft.Maui.ApplicationModel; 
using System; 

namespace AppNomesBr.Pages;

public partial class NovaConsultaNome : ContentPage
{
    private readonly INomesBrService service;
    private readonly INomesBrRepository repository;

    public NovaConsultaNome(INomesBrService service, INomesBrRepository repository)
    {
        InitializeComponent();
        this.service = service;
        this.repository = repository;

       
        var TxtNomeControl = this.FindByName<Entry>("TxtNome");
        var PckSexoControl = this.FindByName<Picker>("PckSexo");
        var BtnPesquisarControl = this.FindByName<Button>("BtnPesquisar");
        var BtnDeleteAllControl = this.FindByName<Button>("BtnDeleteAll");
        var GrdNomesBrControl = this.FindByName<CollectionView>("GrdNomesBr");
        


       
        if (BtnPesquisarControl != null) BtnPesquisarControl.Clicked += BtnPesquisar_Clicked;
        if (BtnDeleteAllControl != null) BtnDeleteAllControl.Clicked += BtnDeleteAll_Clicked;
    }

    private async void BtnDeleteAll_Clicked(object? sender, EventArgs e)
    {
        
        var GrdNomesBrControl = this.FindByName<CollectionView>("GrdNomesBr");

        try
        {
            IsBusy = true;
            var registros = await repository.GetAll();
            if (registros != null) 
            {
                foreach (var registro in registros)
                {
                    if (registro != null) 
                        await repository.Delete(registro.Id);
                }
            }
            await CarregarNomes(); 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERRO] Falha ao deletar todos: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível deletar os registros.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarNomes();
    }

    private async void BtnPesquisar_Clicked(object? sender, EventArgs e)
    {
        
        var TxtNomeControl = this.FindByName<Entry>("TxtNome");
        var PckSexoControl = this.FindByName<Picker>("PckSexo");

        
        if (TxtNomeControl == null) return;

        var nome = TxtNomeControl.Text?.Trim()?.ToUpperInvariant();
        var sexo = PckSexoControl?.SelectedItem?.ToString(); 

        if (!string.IsNullOrWhiteSpace(nome))
        {
            IsBusy = true;
            try
            {
                await service.InserirNovoRegistroNoRanking(nome, sexo);
                await CarregarNomes();

                
                TxtNomeControl.Text = string.Empty;
                if (PckSexoControl != null) PckSexoControl.SelectedItem = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERRO] Falha ao pesquisar/inserir nome: {ex.Message}");
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível adicionar o nome ao ranking.", "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        else
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Atenção", "Por favor, digite um nome para pesquisar.", "OK");
            }
        }

    }

    private async Task CarregarNomes()
    {
        
        var GrdNomesBrControl = this.FindByName<CollectionView>("GrdNomesBr");
        if (GrdNomesBrControl == null) return;

        try
        {
            IsBusy = true;
            var result = await service.ListaMeuRanking();
            GrdNomesBrControl.ItemsSource = result?.FirstOrDefault()?.Resultado ?? new List<Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos.RankingNome>();

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERRO] Falha ao carregar 'Meu Ranking': {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Não foi possível carregar seu ranking.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}